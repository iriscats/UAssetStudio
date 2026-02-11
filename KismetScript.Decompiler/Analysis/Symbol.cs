using System.Diagnostics;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Linker.Decompiler.Analysis;

public class Symbol
{
    private List<Symbol> _children = new();
    private Symbol? _parent;
    private Symbol? _class;
    private Symbol? _super;
    private Symbol? _template;
    private Symbol? _superStruct;
    private Symbol? _innerClass;
    private Symbol? _propertyType;

    public Symbol? Parent
    {
        get => _parent;
        set
        {
            if (!CheckCircularReference(value))
                return; // Skip assignment to prevent circular dependency
            _parent?._children.Remove(this);
            _parent = value;
            _parent?._children.Add(this);
        }
    }
    public IReadOnlyList<Symbol> Children => _children;
    public string Name { get; set; } = string.Empty;

    public SymbolFlags Flags { get; set; }
    public SymbolType Type { get; set; }

    public Import? Import { get; set; }
    public FPackageIndex? ImportIndex { get; set; }

    public virtual Export? Export { get; set; }
    public FPackageIndex? ExportIndex { get; set; }

    public FProperty FProperty { get; set; } = null!;
    public UProperty UProperty { get; set; } = null!;

    public Symbol? Class
    {
        get => _class;
        set
        {
            if (value != _class)
            {
                if (!CheckCircularReferenceRecursively(value, x => x.Class))
                    return; // Skip to prevent circular dependency
                _class = value;
            }
        }
    }
    public Symbol? Super
    {
        get => _super;
        set
        {
            if (value != _super)
            {
                if (!CheckCircularReferenceRecursively(value, x => x.Super))
                    return; // Skip to prevent circular dependency
                _super = value;
            }
        }
    }
    public Symbol? Template 
    { 
        get => _template; 
        set 
        { 
            if (value != _template)
            {
                if (!CheckCircularReferenceRecursively(value, x => x.Template))
                    return;
                _template = value;
            }
        } 
    }
    public Symbol? SuperStruct 
    {
        get => _superStruct; 
        set 
        { 
            if (value != _superStruct)
            {
                if (!CheckCircularReferenceRecursively(value, x => x.SuperStruct))
                    return;
                _superStruct = value;
            }
        } 
    }
    public Symbol? ClassWithin 
    { 
        get => _innerClass; 
        set 
        { 
            if (value != _innerClass)
            {
                if (!CheckCircularReferenceRecursively(value, x => x.ClassWithin))
                    return;
                _innerClass = value;
            }
        } 
    }
    public Symbol? PropertyClass 
    { 
        get => _propertyType; 
        set 
        { 
            if (value != _propertyType)
            {
                if (!CheckCircularReferenceRecursively(value, x => x.PropertyClass))
                    return;
                _propertyType = value;
            }
        } 
    }
    public Symbol? ClonedFrom { get; set; }

    public Symbol? Enum { get; set; }
    public Symbol? UnderlyingProp { get; set; }
    public Symbol? Inner { get; set; }
    public Symbol? ElementProp { get; set; }
    public Symbol? MetaClass { get; set; }
    public Symbol? SignatureFunction { get; set; }
    public Symbol? InterfaceClass { get; set; }
    public Symbol? KeyProp { get; set; }
    public Symbol? ValueProp { get; set; }
    public Symbol? Struct { get; set; }

    public SymbolFunctionMetadata FunctionMetadata { get; set; } = new();
    public SymbolClassMetadata ClassMetadata { get; set; } = new();

    public Symbol()
    {

    }

    /// <summary>
    /// Gets the super class at the root of the class hierarchy.
    /// </summary>
    public Symbol? RootSuperClass
    {
        get
        {
            var currentClass = Super;
            while (currentClass?.Super != null)
                currentClass = currentClass.Super;
            return currentClass;
        }
    }

    public bool CheckSuperClassCircularReference(Symbol? newSuperClass = default)
    {
        var currentClass = this;
        var seenClasses = new HashSet<Symbol>();
        while (currentClass.Super != null)
        {
            if (!seenClasses.Add(currentClass))
                return false; // Cycle detected
            currentClass = currentClass.Super;
        }
        if (newSuperClass != null)
            if (!seenClasses.Add(newSuperClass))
                return false; // Adding this super would create a cycle
        return true;
    }

    /// <summary>
    /// Adds a superclass at the root of the class hierarchy.
    /// Returns false if adding the superclass would create a circular reference.
    /// </summary>
    public bool AddSuperClass(Symbol superClass)
    {
        if (!CheckSuperClassCircularReference(superClass))
            return false;
        var currentClass = RootSuperClass ?? this;
        currentClass.Super = superClass;
        return true;
    }

    private IEnumerable<Symbol> GetAncestors(Func<Symbol, Symbol?> getter)
    {
        var ancestor = getter(this);
        if (ancestor != null)
        {
            yield return ancestor;
            foreach (var subAncestor in GetAncestors(getter))
                yield return subAncestor;
        }
    }

    private bool CheckCircularReference(Symbol? symbol)
    {
        return symbol != this;
    }

    private bool CheckCircularReferenceRecursively(Symbol? symbol, Func<Symbol, Symbol?> getter)
    {
        if (symbol == this)
            return false;

        var ancestor = getter(this);
        if (ancestor != null)
        {
            if (ancestor == symbol)
                return false;
            return ancestor.CheckCircularReferenceRecursively(symbol, getter);
        }
        return true;
    }

    public bool IsClass => 
        Class?.Name == "Class" ||
        Class?.Name == "BlueprintGeneratedClass";

    public bool IsInstance => !IsClass;

    public Symbol? ResolvedType
        => IsClass ? this : PropertyClass ?? InterfaceClass ?? Struct ?? Class;

    public bool IsImport => Import != null;
    public bool IsExport => Export != null;

    private Symbol? FindMember(Func<Symbol, bool> predicate)
    {
        var stack = new Stack<Symbol>();
        var visited = new HashSet<Symbol>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue; // Skip already visited symbols to prevent cycles

            foreach (var child in current.Children)
            {
                if (predicate(child))
                    return child;
            }

            if (current.Super != null)
                stack.Push(current.Super);

            if (current.PropertyClass != null)
                stack.Push(current.PropertyClass);

            if (current.InterfaceClass != null)
                stack.Push(current.InterfaceClass);

            if (current.Struct != null)
                stack.Push(current.Struct);

            if (current.Class != null)
                stack.Push(current.Class);
        }

        return null;
    }

    public bool HasMember(string name)
    {
        return FindMember(x => x.Name == name) != null;
    }

    public bool HasMember(Symbol member)
    {
        return FindMember(x => x == member) != null;
    }

    public Symbol? GetMember(KismetPropertyPointer pointer)
    {
        if (pointer.Old != null)
        {
            return GetMember(pointer.Old);
        }
        else
        {
            return pointer.New != null && pointer.New.Path.Length > 0 ? GetMember(pointer.New.Path[0].ToString()) : null;
        }
    }

    public Symbol? GetMember(FPackageIndex index)
    {
        Debug.Assert(Super != this && PropertyClass != this && InterfaceClass != this && Struct != this && Class != this);

        return Children.Where(x => x.ImportIndex?.Index == index.Index || x.ExportIndex?.Index == index.Index).FirstOrDefault()
            ?? Super?.GetMember(index)
            ?? PropertyClass?.GetMember(index)
            ?? InterfaceClass?.GetMember(index)
            ?? Struct?.GetMember(index)
            ?? Class?.GetMember(index);
    }

    public Symbol? GetMember(string name)
    {
        Debug.Assert(Super != this && PropertyClass != this && InterfaceClass != this && Struct != this && Class != this);

        return Children.Where(x => x.Name == name).FirstOrDefault()
            ?? Super?.GetMember(name)
            ?? PropertyClass?.GetMember(name)
            ?? InterfaceClass?.GetMember(name)
            ?? Struct?.GetMember(name)
            ?? Class?.GetMember(name);
    }

    public bool InheritsClass(Symbol classSymbol)
    {
        if (Super == classSymbol)
            return true;
        return Super?.InheritsClass(classSymbol) ?? false;
    }

    public void AddChild(Symbol child)
    {
        if (child.Parent != this)
        {
            child.Parent = this;
        }
    }

    public void RemoveChild(Symbol child)
    {
        if (child.Parent == this)
        {
            child.Parent = null;
        }
    }

    public void AddChildren(IEnumerable<Symbol> children)
    {
        foreach (var child in children)
        {
            AddChild(child);
        }
    }

    public void RemoveChildren(IEnumerable<Symbol> children)
    {
        foreach (var child in children)
        {
            RemoveChild(child);
        }
    }

    public override string ToString()
    {
        if (PropertyClass != null)
            return $"[{Flags}] {Class?.Name}<{PropertyClass?.Name}> {Name}";
        else if (InterfaceClass != null)
            return $"[{Flags}] {Class?.Name}<{InterfaceClass?.Name}> {Name}";
        else if (Struct != null)
            return $"[{Flags}] {Class?.Name}<{Struct?.Name}> {Name}";
        else
            return $"[{Flags}] {Class?.Name} {Name}";
    }
}

public class SymbolClassMetadata
{
    public bool IsStaticClass { get; set; }
}
