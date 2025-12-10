using KismetScript.Compiler.Compiler.Context;
using KismetScript.Syntax;
using KismetScript.Syntax.Statements;
using KismetScript.Syntax.Statements.Expressions;
using KismetScript.Syntax.Statements.Expressions.Identifiers;
using KismetScript.Syntax.Statements.Expressions.Literals;
using UAssetAPI.Kismet.Bytecode;

namespace KismetScript.Compiler.Compiler;

/// <summary>
/// Partial class containing type and member context resolution methods.
/// </summary>
public partial class KismetScriptCompiler
{
    /// <summary>
    /// Gets the type symbol for a variable.
    /// </summary>
    /// <param name="variableSymbol"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Symbol GetVariableTypeSymbol(VariableSymbol variableSymbol)
    {
        if (variableSymbol.Declaration != null)
        {
            if (variableSymbol.Declaration.Type.IsConstructedType)
            {
                var typeSymbol = GetSymbol<ClassSymbol>(variableSymbol.Declaration.Type.TypeParameter.Text);
                if (typeSymbol != null)
                    return typeSymbol;
                var symbol = GetSymbol<Symbol>(variableSymbol.Declaration.Type.TypeParameter!);
                if (symbol is VariableSymbol typeVariableSymbol)
                    return GetVariableTypeSymbol(typeVariableSymbol);
                return symbol!;
            }
            else
            {
                var typeSymbol = GetSymbol<ClassSymbol>(variableSymbol.Declaration.Type.Text);
                if (typeSymbol != null)
                    return typeSymbol;
                var symbol = GetSymbol<Symbol>(variableSymbol.Declaration.Type);
                if (symbol is VariableSymbol typeVariableSymbol)
                    return GetVariableTypeSymbol(typeVariableSymbol);
                return symbol!;
            }
        }
        else
        {

            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets the member context (owning class) for the given expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private MemberContext GetContextForExpression(Expression expression)
    {
        // Prevent infinite recursion
        if (_contextResolvingExpressions.Contains(expression))
        {
            return new MemberContext()
            {
                Symbol = _classContext.Symbol,
                Type = ContextType.This
            };
        }

        _contextResolvingExpressions.Add(expression);
        try
        {
            var contextSymbol = GetSymbol<Symbol>(expression);
        var contextSymbolTemp = contextSymbol;
        var contextType = ContextType.Class;
        MemberContext subContext = default;
        if (contextSymbol == null)
        {
            if (expression is MemberExpression memberExpression)
            {
                contextType = ContextType.SubContext;
                subContext = GetContextForMemberExpression(memberExpression);
                // Ensure we have a valid symbol after processing the member expression
                if (subContext.Symbol != null)
                {
                    contextSymbol = subContext.Symbol;
                }
                else
                {
                    contextSymbol = _classContext.Symbol;
                    subContext = new MemberContext()
                    {
                        Symbol = _classContext.Symbol,
                        Type = ContextType.This
                    };
                }
            }
            else
            {
                // Handle cases where expression is not a MemberExpression but still has no symbol
                // This can happen with complex expressions or literals
                contextType = ContextType.SubContext;
                subContext = new MemberContext()
                {
                    Symbol = _classContext.Symbol,
                    Type = ContextType.This
                };
                contextSymbol = _classContext.Symbol;
            }
        }
        else if (contextSymbol is VariableSymbol variableSymbol)
        {
            if (variableSymbol.VariableCategory == VariableCategory.This)
            {
                return new MemberContext()
                {
                    Symbol = variableSymbol.DeclaringSymbol,
                    Type = ContextType.This
                };
            }
            else if (variableSymbol.VariableCategory == VariableCategory.Base)
            {
                return new MemberContext()
                {
                    Symbol = variableSymbol.DeclaringSymbol,
                    Type = ContextType.Base
                };
            }

            if (variableSymbol.Declaration != null)
            {
                // TODO: make more flexible
                // Prefer explicit constructed type (e.g. Struct<HitResult>)
                if (variableSymbol.Declaration.Type.IsConstructedType)
                {
                    contextType = variableSymbol.Declaration.Type.Text switch
                    {
                        "Struct" => ContextType.Struct,
                        "Interface" => ContextType.Interface,
                        "Class" => ContextType.Class,
                        "Object" => ContextType.Object,
                        _ => throw new NotImplementedException()
                    };
                }
                else
                {
                    // Fallback based on resolved ValueKind when not a constructed type
                    // This helps correctly classify variables declared directly as struct types (e.g. HitResult)
                   switch (variableSymbol.Declaration.Type.ValueKind)
                    {
                        case ValueKind.Struct:
                            contextType = ContextType.Struct;
                            break;
                        case ValueKind.Interface:
                            contextType = ContextType.Interface;
                            break;
                        case ValueKind.Class:
                            contextType = ContextType.Class;
                            break;
                        case ValueKind.Object:
                            contextType = ContextType.Object;
                            break;
                        default:
                            // Heuristic: if inner symbol name suggests a known struct (e.g. Engine.HitResult), treat as struct
                            // This heuristic avoids emitting EX_Context for struct member access like CurrentFloor.HitResult
                            if (variableSymbol.InnerSymbol is ClassSymbol innerClass)
                            {
                                var name = innerClass.Name ?? string.Empty;
                                if (name.EndsWith("HitResult", StringComparison.InvariantCulture))
                                {
                                    contextType = ContextType.Struct;
                                }
                            }
                            break;
                    }
                    //if (variableSymbol.IsExternal)
                    //{
                    //    contextType = ContextType.ObjectConst;
                    //}
                }

                contextSymbol = GetVariableTypeSymbol(variableSymbol);

                // TODO check base type
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (contextSymbol is ClassSymbol classSymbol)
        {
            if (classSymbol == _classContext.Symbol)
            {
                contextType = ContextType.This;
            }
            else if (classSymbol == _classContext.Symbol.BaseSymbol)
            {
                contextType = ContextType.Base;
            }
            else
            {
                contextType = ContextType.Class;
            }
        }
        else if (contextSymbol is EnumSymbol enumSymbol)
        {
            contextType = ContextType.Enum;
        }

        // Fallback for cases where contextSymbol is still null
        if (contextSymbol == null)
        {
            contextSymbol = _classContext.Symbol;
            contextType = ContextType.This;
        }

        var context = new MemberContext()
        {
            SubContext = subContext,
            Symbol = contextSymbol,
            Type = contextType,
        };

        if (expression is Identifier contextIdentifier &&
            contextIdentifier.Text == _classContext?.Symbol.Name)
        {
            // TODO: make more flexible
            // Explicit virtual method call
            context.CallVirtualFunctionAsFinal = true;
        }

        return context;
        }
        finally
        {
            _contextResolvingExpressions.Remove(expression);
        }
    }

    /// <summary>
    /// Gets the member context (owning class) for the given member access expression.
    /// </summary>
    /// <param name="memberExpression"></param>
    /// <returns></returns>
    private MemberContext GetContextForMemberExpression(MemberExpression memberExpression)
    {
        // Add debugging to prevent infinite recursion
        var result = GetContextForExpression(memberExpression.Context);
        return result;
    }

    /// <summary>
    /// Gets the identifier of the member being accessed in the expression.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="isVirtual"></param>
    /// <returns></returns>
    private (Identifier? Identifier, bool IsLookup) GetMemberIdentifier(Expression expression, bool? isVirtual = null)
    {
        if (expression is StringLiteral stringLiteral)
        {
            return (new(stringLiteral.Value), isVirtual ?? false);
        }
        else if (expression is Identifier identifier)
        {
            return (identifier, isVirtual ?? false);
        }
        else if (expression is CallOperator callOperator)
        {
            if (IsIntrinsicFunction(callOperator.Identifier.Text))
            {
                var token = GetInstrinsicFunctionToken(callOperator.Identifier.Text);
                if (token == EExprToken.EX_LocalVariable ||
                    token == EExprToken.EX_InstanceVariable ||
                    token == EExprToken.EX_LocalOutVariable ||
                    token ==  EExprToken.EX_CallMath ||
                    token == EExprToken.EX_FinalFunction ||
                    token == EExprToken.EX_LocalFinalFunction ||
                    token == EExprToken.EX_StructMemberContext)
                {
                    return GetMemberIdentifier(callOperator.Arguments[0].Expression, isVirtual ?? false);
                }
                if (token == EExprToken.EX_VirtualFunction ||
                    token == EExprToken.EX_LocalVirtualFunction)
                {
                    return GetMemberIdentifier(callOperator.Arguments[0].Expression, true);
                }
            }
        }
        else if (expression is MemberExpression memberAccessExpression)
        {
            var context = GetContextForMemberExpression(memberAccessExpression);
            PushContext(context);
            try
            {
                return GetMemberIdentifier(memberAccessExpression.Member);
            }
            finally
            {
                PopContext();
            }
        }

        return (null, false);
    }
}
