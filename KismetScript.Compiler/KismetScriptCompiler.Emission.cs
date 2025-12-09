using KismetScript.Compiler.Compiler.Context;
using KismetScript.Compiler.Compiler.Intermediate;
using KismetScript.Syntax;
using KismetScript.Syntax.Statements;
using KismetScript.Syntax.Statements.Expressions;
using KismetScript.Syntax.Statements.Expressions.Binary;
using KismetScript.Syntax.Statements.Expressions.Unary;
using KismetScript.Utilities;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;

namespace KismetScript.Compiler.Compiler;

/// <summary>
/// Partial class containing code generation, bytecode emission, and fixup functionality.
/// </summary>
public partial class KismetScriptCompiler
{
    /// <summary>
    /// Ensures the EX_Return and EX_EndOfScript instructions are present at the end of the script bytecode, and creates them if they don't exist.
    /// </summary>
    private void EnsureEndOfScriptPresent()
    {
        var beforeLastExpr = _functionContext.PrimaryExpressions
            .Skip(_functionContext.PrimaryExpressions.Count - 1)
            .FirstOrDefault();
        var lastExpr = _functionContext.PrimaryExpressions.LastOrDefault();

        if (beforeLastExpr?.CompiledExpressions.Single() is not EX_Return &&
            lastExpr?.CompiledExpressions.Single() is not EX_Return)
        {
            if (_functionContext.ReturnVariable != null)
            {
                EmitPrimaryExpression(null, new EX_Return()
                {
                    ReturnExpression = Emit(null, new EX_LocalOutVariable()
                    {
                        Variable = GetPropertyPointer(_functionContext.ReturnVariable)
                    }).CompiledExpressions.Single(),
                });
            }
            else
            {
                EmitPrimaryExpression(null, new EX_Return()
                {
                    ReturnExpression = Emit(null, new EX_Nothing()).CompiledExpressions.Single(),
                });
            }
        }

        if (lastExpr?.CompiledExpressions.Single() is not EX_EndOfScript)
        {
            EmitPrimaryExpression(null, new EX_EndOfScript());
        }
    }

    /// <summary>
    /// Commits the compiled expression to the compiled function context.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="expressionState"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitPrimaryExpression(SyntaxNode? syntaxNode, CompiledExpressionContext expressionState)
    {
        _functionContext.AllExpressions.Add(expressionState);
        _functionContext.PrimaryExpressions.Add(expressionState);
        _functionContext.CodeOffset += KismetExpressionSizeCalculator.CalculateExpressionSize(expressionState.CompiledExpressions);
        return expressionState;
    }

    /// <summary>
    /// Commits the expression to the compiled function context.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="expression"></param>
    /// <param name="referencedLabels"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitPrimaryExpression(SyntaxNode? syntaxNode, KismetExpression expression, IEnumerable<LabelSymbol>? referencedLabels = null)
    {
        var expressionState = Emit(syntaxNode, expression, referencedLabels);
        _functionContext.AllExpressions.Add(expressionState);
        _functionContext.PrimaryExpressions.Add(expressionState);
        _functionContext.CodeOffset += KismetExpressionSizeCalculator.CalculateExpressionSize(expressionState.CompiledExpressions);
        return expressionState;
    }

    /// <summary>
    /// Performs any necessary code offset fixups after compilation.
    /// </summary>
    private void DoFixups()
    {
        foreach (var expression in _functionContext.AllExpressions)
        {
            foreach (var compiledExpression in expression.CompiledExpressions)
            {
                switch (compiledExpression)
                {
                    case EX_Jump compiledExpr:
                        compiledExpr.CodeOffset = (uint)expression.ReferencedLabels[0].CodeOffset.Value;
                        break;
                    case EX_JumpIfNot compiledExpr:
                        compiledExpr.CodeOffset = (uint)expression.ReferencedLabels[0].CodeOffset.Value;
                        break;
                    case EX_ClassContext compiledExpr:
                        compiledExpr.Offset = (uint)KismetExpressionSizeCalculator.CalculateExpressionSize(compiledExpr.ContextExpression);
                        break;
                    case EX_Skip compiledExpr:
                        compiledExpr.CodeOffset = (uint)expression.ReferencedLabels[0].CodeOffset.Value;
                        break;
                    // EX_Context_FailSilent
                    case EX_Context compiledExpr:
                        compiledExpr.Offset = (uint)KismetExpressionSizeCalculator.CalculateExpressionSize(compiledExpr.ContextExpression);
                        break;
                    case EX_PushExecutionFlow compiledExpr:
                        compiledExpr.PushingAddress = (uint)expression.ReferencedLabels[0].CodeOffset.Value;
                        break;
                    case EX_SkipOffsetConst compiledExpr:
                        compiledExpr.Value = (uint)expression.ReferencedLabels[0].CodeOffset.Value;
                        break;
                    case EX_SwitchValue compiledExpr:
                        compiledExpr.EndGotoOffset = (uint)expression.ReferencedLabels[0].CodeOffset.Value;
                        for (int i = 0; i < compiledExpr.Cases.Length; i++)
                        {
                            compiledExpr.Cases[i].NextOffset = (uint)expression.ReferencedLabels[i + 1].CodeOffset.Value;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Emits a EX_CallMath call (static native library function call)
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="library"></param>
    /// <param name="name"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitCallMath(Expression expression, string library, string name, IEnumerable<KismetExpression> arguments)
    {
        var librarySymbol = RootScope.GetSymbol<ClassSymbol>(library);
        if (librarySymbol == null)
        {
            librarySymbol = new ClassSymbol(null)
            {
                DeclaringSymbol = new PackageSymbol()
                {
                    DeclaringSymbol = null,
                    IsExternal = true,
                    Name = "/Script/Engine"
                },
                IsExternal = true,
                Name = library,
            };
        }

        var functionSymbol = librarySymbol.GetSymbol<ProcedureSymbol>(name);
        if (functionSymbol == null)
        {
            functionSymbol = new ProcedureSymbol(null)
            {
                DeclaringSymbol = librarySymbol,
                IsExternal = true,
                Name = name
            };
        }

        return Emit(expression, new EX_CallMath()
        {
            StackNode = new IntermediatePackageIndex(functionSymbol),
            Parameters = arguments.ToArray()
        });
    }

    /// <summary>
    /// Emits a call to the KismetArrayLibrary.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="name"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitKismetArrayLibraryCall(Expression expression, string name, IEnumerable<KismetExpression> arguments)
        => EmitCallMath(expression, "KismetArrayLibrary", name, arguments);

    /// <summary>
    /// Emits a call to the KismetStringLibrary.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="name"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitKismetStringLibraryCall(Expression expression, string name, IEnumerable<KismetExpression> arguments)
        => EmitCallMath(expression, "KismetStringLibrary", name, arguments);

    /// <summary>
    /// Emits a call to the KismetMathLibrary.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="name"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitKismetMathLibraryCall(Expression expression, string name, IEnumerable<KismetExpression> arguments)
        => EmitCallMath(expression, "KismetMathLibrary", name, arguments);

    /// <summary>
    /// Emits a call to the KismetMathLibrary.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitKismetMathLibraryCall(UnaryExpression expression, string name)
        => EmitCallMath(expression, "KismetMathLibrary", name, new[] { CompileSubExpression(expression.Operand) });

    /// <summary>
    /// Emits a call to the KismetMathLibrary.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitKismetMathLibraryCall(BinaryExpression expression, string name)
        => EmitCallMath(expression, "KismetMathLibrary", name, new[] { CompileSubExpression(expression.Left), CompileSubExpression(expression.Right) });

    /// <summary>
    /// Emits a call to the KismetTextLibrary.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="name"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    private CompiledExpressionContext EmitKismetTextLibraryCall(Expression expression, string name, IEnumerable<KismetExpression> arguments)
        => EmitCallMath(expression, "KismetTextLibrary", name, arguments);

    /// <summary>
    /// Emits a compiled kismet expression.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="expression"></param>
    /// <param name="expression2"></param>
    /// <param name="referencedLabels"></param>
    /// <returns></returns>
    private CompiledExpressionContext Emit(SyntaxNode? syntaxNode, KismetExpression expression, KismetExpression expression2, IEnumerable<LabelSymbol>? referencedLabels = null)
    {
        return new CompiledExpressionContext()
        {
            SyntaxNode = syntaxNode,
            CodeOffset = _functionContext.CodeOffset,
            CompiledExpressions = new() { expression, expression2 },
            ReferencedLabels = referencedLabels?.ToList() ?? new()
        };
    }

    /// <summary>
    /// Emits a compiled kismet expression.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <param name="expression"></param>
    /// <param name="referencedLabels"></param>
    /// <returns></returns>
    private CompiledExpressionContext Emit(SyntaxNode? syntaxNode, KismetExpression expression, IEnumerable<LabelSymbol>? referencedLabels = null)
    {
        return new CompiledExpressionContext()
        {
            SyntaxNode = syntaxNode,
            CodeOffset = _functionContext.CodeOffset,
            CompiledExpressions = new() { expression },
            ReferencedLabels = referencedLabels?.ToList() ?? new()
        };
    }
}
