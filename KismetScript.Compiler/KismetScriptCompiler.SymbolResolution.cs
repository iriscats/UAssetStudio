using KismetScript.Compiler.Compiler.Context;
using KismetScript.Compiler.Compiler.Exceptions;
using KismetScript.Syntax;
using KismetScript.Syntax.Statements;
using KismetScript.Syntax.Statements.Declarations;
using KismetScript.Syntax.Statements.Expressions;
using KismetScript.Syntax.Statements.Expressions.Identifiers;
using KismetScript.Syntax.Statements.Expressions.Literals;
using UAssetAPI.Kismet.Bytecode;
using System.Linq;

namespace KismetScript.Compiler.Compiler;

/// <summary>
/// Partial class containing symbol table lookup, scope management, and context stack operations.
/// </summary>
public partial class KismetScriptCompiler
{
    /// <summary>
    /// Pushes a new variable scope.
    /// </summary>
    /// <param name="declaringSymbol"></param>
    private void PushScope(Symbol? declaringSymbol)
        => _scopeStack.Push(new(_scopeStack.Peek(), declaringSymbol));

    /// <summary>
    /// Pops the variable scope.
    /// </summary>
    /// <returns></returns>
    private Scope PopScope()
        => _scopeStack.Pop();

    /// <summary>
    /// Pushes a new member context to the stack.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="symbol"></param>
    private void PushContext(ContextType type, Symbol symbol)
        => _contextStack.Push(new MemberContext() { Type = type, Symbol = symbol });

    /// <summary>
    /// Pushes a new member context to the stack.
    /// </summary>
    /// <param name="context"></param>
    private void PushContext(MemberContext context)
    => _contextStack.Push(context);

    /// <summary>
    /// Pops the current member context off the stack.
    /// </summary>
    /// <returns></returns>
    private MemberContext PopContext()
        => _contextStack.Pop();

    /// <summary>
    /// Pushes an RValue to the stack.
    /// </summary>
    /// <param name="rvalue"></param>
    private void PushRValue(KismetPropertyPointer rvalue)
        => _rvalueStack.Push(rvalue);

    /// <summary>
    /// Pops an RValue off the stack.
    /// </summary>
    /// <returns></returns>
    private KismetPropertyPointer PopRValue()
        => _rvalueStack.Pop();

    /// <summary>
    /// Gets a symbol either from the current function variable scope, or from a class instance scope depending on the context
    /// </summary>
    /// <param name="name"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private Symbol? GetSymbol(string name, MemberContext? context = default)
    {
        context ??= Context;
        var memberContext = context?.GetSymbol(name);
        var scopeSymbol = CurrentScope.GetSymbol(name);
        if (IsImplicitThisContext())
        {
            // Symbols in the current scope have priority over symbols that
            // are part of the implicit class instance scope (eg. local variables have priority over instance members).
            return scopeSymbol ?? memberContext;
        }
        else
        {
            // If we're in an explicit class instance scope (eg. accessing class instance members through a property of a class type), this scope
            // should take priority over local variables and such.
            return memberContext ?? scopeSymbol;
        }
    }

    /// <summary>
    /// Gets a symbol either from the current function variable scope, or from a class instance scope depending on the context
    /// </summary>
    /// <param name="name"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private T? GetSymbol<T>(string name, MemberContext? context = default) where T : Symbol
    {
        context ??= Context;
        var memberContext = context?.GetSymbol<T>(name);
        var scopeSymbol = CurrentScope.GetSymbol<T>(name);
        if (IsImplicitThisContext())
        {
            // Symbols in the current scope have priority over symbols that
            // are part of the implicit class instance scope (eg. local variables have priority over instance members).
            return scopeSymbol ?? memberContext;
        }
        else
        {
            // If we're in an explicit class instance scope (eg. accessing class instance members through a property of a class type), this scope
            // should take priority over local variables and such.
            return memberContext ?? scopeSymbol;
        }
    }

    /// <summary>
    /// Gets the symbol associated with the given declaration, according to scoping rules.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="declaration"></param>
    /// <returns></returns>
    /// <exception cref="CompilationError"></exception>
    private T GetRequiredSymbol<T>(Declaration declaration) where T : Symbol
    {
        var symbol = _functionContext?.Symbol.GetSymbol<T>(declaration);
        if (symbol == null)
            symbol = _classContext?.Symbol.GetSymbol<T>(declaration);
        if (symbol == null)
            symbol = CurrentScope.GetSymbol<T>(declaration);

        return symbol ?? throw new CompilationError(declaration.Identifier, $"The name {declaration.Identifier.Text} does not exist in the current context");
    }

    /// <summary>
    /// Returns if the current context is an implicit class instance member context (eg. this.foo can also be implicitly accessed as just foo)
    /// </summary>
    /// <returns></returns>
    private bool IsImplicitThisContext()
    {
        return Context?.Type == ContextType.This &&
                    Context?.IsImplicit == true;
    }

    /// <summary>
    /// Declares a symbol in the current scope.
    /// </summary>
    /// <param name="symbol"></param>
    private void DeclareSymbol(Symbol symbol)
        => CurrentScope.DeclareSymbol(symbol);

    /// <summary>
    /// Derives the referenced symbol name from the given expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private string? GetSymbolName(Expression expression)
    {
        if (expression is TypeIdentifier typeIdentifier)
        {
            if (typeIdentifier.IsConstructedType)
            {
                // TODO handle base type info (Struct<>, Array<>, etc)
                return GetSymbolName(typeIdentifier.TypeParameter!);
            }
            else
            {
                return typeIdentifier.Text;
            }
        }
        else if (expression is StringLiteral stringLiteral)
        {
            return stringLiteral.Value;
        }
        else if (expression is Identifier identifier)
        {
            return identifier.Text;
        }
        else if (expression is MemberExpression memberExpression)
        {
            PushContext(GetContextForMemberExpression(memberExpression));
            try
            {
                if (Context.Type != ContextType.This &&
                    Context.Type != ContextType.Base)
                {
                    // Context is not a symbol, but a sub-context
                    return null;
                }
                else
                {
                    return GetSymbolName(memberExpression.Member);
                }
            }
            finally
            {
                PopContext();
            }
        }
        else if (expression is CallOperator callOperator)
        {
            if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_ArrayGetByRef))
            {
                // 0: Array, 1: Index
                var arrayObject = callOperator.Arguments.First();
                return GetSymbolName(arrayObject.Expression);
            }
            else if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_InstanceVariable))
            {
                return GetSymbolName(callOperator.Arguments.First().Expression);
            }
            else if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_SwitchValue))
            {
                var defaultTerm = callOperator.Arguments[2];
                return GetSymbolName(defaultTerm.Expression);
            }
            else if (IsIntrinsicTokenAny(callOperator.Identifier.Text,
                EExprToken.EX_FinalFunction, EExprToken.EX_VirtualFunction,
                EExprToken.EX_LocalFinalFunction, EExprToken.EX_LocalVirtualFunction))
            {
                // For function calls, get the symbol name from the function name (first argument)
                return GetSymbolName(callOperator.Arguments.First().Expression);
            }
            else if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_Context))
            {
                // For Context, get the symbol name from the object expression (first argument)
                return GetSymbolName(callOperator.Arguments.First().Expression);
            }
            else
            {
                throw new NotImplementedException($"GetSymbolName not implemented for CallOperator: {callOperator.Identifier.Text}");
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets a symbol of a specific type associated with the expression.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Symbol? GetSymbol<T>(Expression expression) where T : Symbol
    {
        if (expression is TypeIdentifier typeIdentifier)
        {
            if (typeIdentifier.IsConstructedType)
            {
                // TODO handle base type info (Struct<>, Array<>, etc)
                return GetSymbol<T>(typeIdentifier.TypeParameter!);
            }
            else
            {
                return GetSymbol<T>(typeIdentifier.Text);
            }
        }
        else if (expression is StringLiteral stringLiteral)
        {
            return GetSymbol<T>(stringLiteral.Value);
        }
        else if (expression is Identifier identifier)
        {
            return GetSymbol<T>(identifier.Text);
        }
        else if (expression is MemberExpression memberExpression)
        {
            PushContext(GetContextForMemberExpression(memberExpression));
            try
            {
                return GetSymbol<T>(memberExpression.Member);
            }
            finally
            {
                PopContext();
            }
        }
        else if (expression is CallOperator callOperator)
        {
            if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_ArrayGetByRef))
            {
                // 0: Array, 1: Index
                var arrayObject = callOperator.Arguments.First();
                return GetSymbol<T>(arrayObject.Expression);
            }
            else if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_InstanceVariable))
            {
                if (Context == null)
                {
                    PushContext(ContextType.This, _classContext.Symbol);
                    try
                    {
                        return GetSymbol<T>(callOperator.Arguments.First().Expression);
                    }
                    finally
                    {
                        PopContext();
                    }
                }
                else
                {
                    return GetSymbol<T>(callOperator.Arguments.First().Expression);
                }
            }
            else if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_SwitchValue))
            {
                var defaultTerm = callOperator.Arguments[2];
                return GetSymbol<T>(defaultTerm.Expression);
            }
            else if (IsIntrinsicTokenAny(callOperator.Identifier.Text,
                EExprToken.EX_FinalFunction, EExprToken.EX_VirtualFunction,
                EExprToken.EX_LocalFinalFunction, EExprToken.EX_LocalVirtualFunction))
            {
                // For function calls, get the symbol from the function name (first argument)
                return GetSymbol<T>(callOperator.Arguments.First().Expression);
            }
            else if (IsIntrinsicToken(callOperator.Identifier.Text, EExprToken.EX_Context))
            {
                // For Context, get the symbol from the object expression (first argument)
                return GetSymbol<T>(callOperator.Arguments.First().Expression);
            }
            else
            {
                throw new NotImplementedException($"GetSymbol<T> not implemented for CallOperator: {callOperator.Identifier.Text}");
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
