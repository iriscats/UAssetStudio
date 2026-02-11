using KismetScript.Compiler.Compiler.Context;
using KismetScript.Compiler.Compiler.Exceptions;
using KismetScript.Compiler.Compiler.Intermediate;
using KismetScript.Syntax;
using KismetScript.Syntax.Statements;
using KismetScript.Syntax.Statements.Declarations;
using KismetScript.Syntax.Statements.Expressions;
using KismetScript.Syntax.Statements.Expressions.Identifiers;
using KismetScript.Syntax.Statements.Expressions.Literals;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Compiler.Compiler;

/// <summary>
/// Partial class containing package index and property pointer resolution methods.
/// </summary>
public partial class KismetScriptCompiler
{
    /// <summary>
    /// Gets the package index associated with the expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private FPackageIndex GetPackageIndex(Expression expression, MemberContext? context = default)
    {
        if (expression is StringLiteral stringLiteral)
        {
            return GetPackageIndex(stringLiteral, stringLiteral.Value, context);
        }
        else if (expression is Identifier identifier)
        {
            return GetPackageIndex(identifier, identifier.Text, context);
        }
        else if (expression is MemberExpression memberExpression)
        {
            PushContext(GetContextForMemberExpression(memberExpression));
            try
            {
                return GetPackageIndex(memberExpression.Member);
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
                var arrayObject = callOperator.Arguments.First();
                return GetPackageIndex(arrayObject);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets the package index associated with the symbol.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    private FPackageIndex? GetPackageIndex(Symbol symbol)
    {
        return new IntermediatePackageIndex(symbol);
    }

    /// <summary>
    /// Gets the package index for the given symbol name.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="name"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private FPackageIndex? GetPackageIndex(SyntaxNode syntaxNode, string name, MemberContext? context = default)
    {
        // TODO fix
        if (name == "<null>")
            return new FPackageIndex(0);

        var symbol = GetSymbol(name, context);
        // If not found in the specific context, also try the class hierarchy
        // This prevents creating placeholders that shadow correctly declared symbols in base classes
        if (symbol == null)
        {
            symbol = _classContext?.Symbol?.GetSymbol<ProcedureSymbol>(name);
        }
        if (symbol == null)
        {
            // Create a placeholder symbol for external/unknown members
            // This allows compilation to continue for functions/properties that are defined in engine classes
            var declaringSymbol = context?.Symbol ?? _classContext.Symbol;

            // Create a placeholder procedure symbol
            symbol = new ProcedureSymbol(new ProcedureDeclaration()
            {
                Identifier = new Identifier() { Text = name },
                Modifiers = ProcedureModifier.Sealed
            })
            {
                Name = name,
                IsExternal = true,
                DeclaringSymbol = declaringSymbol
            };
        }

        return new IntermediatePackageIndex(symbol);
    }

    /// <summary>
    /// Gets the package index for the given argument.
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private FPackageIndex GetPackageIndex(Argument argument)
        => GetPackageIndex(argument.Expression);

    /// <summary>
    /// Tries to get the property pointer associated with the expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="pointer"></param>
    /// <returns></returns>
    private bool TryGetPropertyPointer(Expression expression, out KismetPropertyPointer pointer)
    {
        pointer = null;

        if (expression is StringLiteral stringLiteral)
        {
            pointer = GetPropertyPointer(stringLiteral, stringLiteral.Value);
        }
        else if (expression is Identifier identifier)
        {
            pointer = GetPropertyPointer(identifier, identifier.Text);
        }
        else if (expression is CallOperator callOperator)
        {
            if (IsIntrinsicFunction(callOperator.Identifier.Text))
            {
                var token = GetInstrinsicFunctionToken(callOperator.Identifier.Text);
                if (token == EExprToken.EX_LocalVariable ||
                    token == EExprToken.EX_InstanceVariable ||
                    token == EExprToken.EX_LocalOutVariable)
                {
                    if (token == EExprToken.EX_InstanceVariable && Context == null)
                    {
                        PushContext(ContextType.This, _classContext.Symbol);
                        try
                        {
                            pointer = GetPropertyPointer(callOperator.Arguments[0]);
                        }
                        finally
                        {
                            PopContext();
                        }
                    }
                    else
                    {
                        pointer = GetPropertyPointer(callOperator.Arguments[0]);
                    }
                }
                else if (token == EExprToken.EX_StructMemberContext)
                {
                    pointer = GetPropertyPointer(callOperator.Arguments[0]);
                }
                else if (token == EExprToken.EX_Context)
                {
                    // For EX_Context, the second argument contains the property reference
                    // e.g., EX_Context(objectExpr, EX_InstanceVariable("PropertyName"))
                    if (callOperator.Arguments.Count >= 2)
                    {
                        TryGetPropertyPointer(callOperator.Arguments[1].Expression, out pointer);
                    }
                }
            }
        }
        else if (expression is MemberExpression memberAccessExpression)
        {
            var context = GetContextForMemberExpression(memberAccessExpression);
            PushContext(context);
            try
            {
                pointer = GetPropertyPointer(memberAccessExpression.Member);
            }
            finally
            {
                PopContext();
            }
        }

        return pointer != null;
    }

    /// <summary>
    /// Gets the property pointer associated with the expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="UnexpectedSyntaxError"></exception>
    private KismetPropertyPointer GetPropertyPointer(Expression expression)
    {
        if (!TryGetPropertyPointer(expression, out var pointer))
            throw new UnexpectedSyntaxError(expression);
        return pointer;
    }

    /// <summary>
    /// Gets a required symbol by name.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="name"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="CompilationError"></exception>
    private Symbol GetRequiredSymbol(SyntaxNode syntaxNode, string name, MemberContext? context = default)
    {
        var symbol = GetSymbol(name, context) ?? throw new CompilationError(syntaxNode, $"The name {name} does not exist in the current context");
        return symbol;
    }

    /// <summary>
    /// Gets a required symbol by name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="syntaxNode"></param>
    /// <param name="name"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="CompilationError"></exception>
    private T GetRequiredSymbol<T>(SyntaxNode syntaxNode, string name, MemberContext? context = default) where T : Symbol
    {
        var symbol = GetSymbol(name, context) ?? throw new CompilationError(syntaxNode, $"The name {name} does not exist in the current context");
        var castedSymbol = symbol as T ?? throw new CompilationError(syntaxNode, $"Expected a {typeof(T).Name}, got {symbol.GetType().Name}");
        return castedSymbol;
    }

    /// <summary>
    /// Gets a property pointer by name.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private KismetPropertyPointer GetPropertyPointer(SyntaxNode syntaxNode, string name)
    {
        var symbol = GetSymbol(name);
        if (symbol == null)
        {
            // For unknown types in EX_ArrayConst, create a fallback symbol
            // This handles cases like GameplayTags where the type isn't explicitly defined
            symbol = new UnknownSymbol()
            {
                Name = name,
                DeclaringSymbol = null,
                IsExternal = false,
            };
        }
        return new IntermediatePropertyPointer(symbol);
    }

    /// <summary>
    /// Gets a property pointer by symbol.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    private KismetPropertyPointer GetPropertyPointer(Symbol symbol)
        => new IntermediatePropertyPointer(symbol);

    /// <summary>
    /// Gets a property pointer by argument.
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private KismetPropertyPointer GetPropertyPointer(Argument argument)
    {
        return GetPropertyPointer(argument.Expression);
    }

    /// <summary>
    /// Compiles the expression directly into a KismetExpression.
    /// </summary>
    /// <param name="right"></param>
    /// <returns></returns>
    private KismetExpression CompileSubExpression(Expression right)
    {
        return CompileExpression(right).CompiledExpressions.Single();
    }

    /// <summary>
    /// Compiles the argument directly to a KismetExpression.
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private KismetExpression CompileSubExpression(Argument argument)
    {
        var expressionState = CompileExpression(argument.Expression);
        return expressionState.CompiledExpressions.Single();
    }
}
