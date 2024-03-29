using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;

public class Compiler {
    public bool PRINT_AST = false;
    public bool PRINT_STEPS = false;
    public bool SHOW_TIMINGS = false;
    public bool CATCH_ERRS = true;

    Stopwatch watch;

    public CompilationResultStatus Compile(string file, string text) {
        if (CATCH_ERRS) {
            try {
                _Compile(file, text);
                return CompilationResultStatus.GOOD;
            } catch (SyntaxErrorException e) {
                ShowCompilationError(e, text);
                return CompilationResultStatus.USERERR;
            } catch (TargetInvocationException e) {
                Exception inner = e.InnerException;
                if (inner is SyntaxErrorException) {
                    ShowCompilationError((SyntaxErrorException)inner, text);
                    return CompilationResultStatus.USERERR;
                } else {
                    ExceptionDispatchInfo.Capture(inner).Throw();
                    return CompilationResultStatus.FAIL;
                }
            } catch (PythonExceptionException e) {
                Console.WriteLine("Error in Python code:");
                Console.WriteLine(e.Message);
                return CompilationResultStatus.FAIL;
            } catch (BashExceptionException e) {
                Console.WriteLine("Error in Bash code:");
                Console.WriteLine(e.Message);
                return CompilationResultStatus.FAIL;
            }
        } else {
            _Compile(file, text);
            return CompilationResultStatus.GOOD;
        }
    }
    
    void ShowCompilationError(SyntaxErrorException e, string text) {
        CodeSpan span = e.span;

        int startLine = 1;
        int endLine = 1;
        int totalLines = 1;
        int stage = 0;
        int startIndex = 0;
        
        List<string> lines = new List<string> {""};
        
        for (int i = 0; i < text.Length; i++) {
            if (stage == 0 && i == span.GetStart()) {
                stage = 1;
            } else if (stage == 1 && i == span.GetEnd()+1) {
                stage = 2;
            }
            char chr = text[i];
            if (chr == '\n') {
                if (stage == 0) startIndex = 0;
                if (stage <= 0)
                    startLine++;
                if (stage <= 1)
                    endLine++;
                totalLines++;
                lines.Add("");
            } else {
                lines[lines.Count-1] += chr;
                if (stage == 0) startIndex++;
            }
        }
        
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write("compilation error: ");
        Console.ResetColor();
        Console.WriteLine(e.Message);
        
        Console.Write(startLine == endLine ? "Line " : "Lines ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(startLine);
        Console.ResetColor();
        
        if (startLine == endLine) {
            Console.WriteLine();
            
            string linenum = startLine.ToString();
            string line = lines[startLine-1];
            while (line.Length > 0 && Utils.Whitespace.Contains(line[0])) {
                line = line.Substring(1);
                startIndex--;
            }
            Console.WriteLine(line);
            Console.Write(new string(' ', startIndex));
            Console.ForegroundColor = ConsoleColor.Green;
            if (span.Size() == 1) {
                Console.Write("^");
            } else {
                Console.Write("┗");
                for (int i = 0; i < span.Size()-2; i++)
                    Console.Write("━");
                Console.Write("┛");
            }
            Console.ResetColor();
            Console.WriteLine();
        } else {
            Console.Write("–");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(endLine);
            Console.ResetColor();

            int firstLine = Math.Max(1, startLine-1);
            int lastLine = Math.Min(lines.Count, endLine+1);

            int prefixLen = lastLine.ToString().Length + 1;

            for (int line = firstLine; line <= lastLine; line++) {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(line.ToString().PadRight(prefixLen));
                Console.ResetColor();
                string prefix = "  ";
                if (line == startLine) {
                    prefix = "┏╸";
                } else if (line == endLine) {
                    prefix = "┗╸";
                } else if (line > startLine && line < endLine) {
                    prefix = "┃ ";
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(prefix);
                Console.ResetColor();
                Console.WriteLine(lines[line-1]);
            }
        }
    }

    void TimingStep() {
        if (SHOW_TIMINGS) {
            Console.WriteLine((watch.ElapsedMilliseconds/1000.0).ToString());
            watch.Restart();
        }
    }

    void Step(string text) {
        if (PRINT_STEPS) {
            Console.WriteLine(text);
        }
    }

    void _Compile(string path, string text) {
        Program program = new Program(
            Path.GetFullPath(path), new List<IToken>()
        );
        program.span = new CodeSpan(0, text.Length-1);
        int i = 0;
        foreach (char chr in text) {
            TextToken token = new TextToken(chr.ToString());
            token.span = new CodeSpan(i);
            program.Add(token);
            i++;
        }

        watch = new Stopwatch();
        if (SHOW_TIMINGS) watch.Start();

        Step("Tokenizing strings...");
        program = TokenizeStrings(program);
        TimingStep();

        Step("Tokenizing character constants...");
        program = TokenizeCharacterConstants(program);
        TimingStep();

        Step("Removing comments...");
        program = RemoveComments(program);
        TimingStep();

        Step("Tokenizing function signatures...");
        program = TokenizeFuncSignatures(program);
        TimingStep();

        Step("Tokenizing names...");
        program = TokenizeNames(program);
        TimingStep();

        Step("Tokenizing keywords...");
        program = TokenizeKeywords(program);
        TimingStep();

        Step("Tokenizing numbers...");
        program = TokenizeNumbers(program);
        TimingStep();

        Step("Removing whitespace...");
        program = RemoveWhitespace(program);
        TimingStep();

        Step("Tokenizing blocks...");
        program = TokenizeBlocks(program);
        TimingStep();

        Step("Tokenizing function arguments...");
        program = TokenizeFuncArguments(program);
        TimingStep();

        Step("Tokenizing functions...");
        program = TokenizeFunctionHolders(program);
        TimingStep();

        Step("Tokenizing structs...");
        program = TokenizeStructHolders(program);
        TimingStep();

        Step("Converting function blocks...");
        program = ConvertFunctionBlocks(program);
        TimingStep();

        Step("Computing base types_...");
        ComputeBaseTypes_(program);
        TimingStep();

        Step("Tokenizing base types...");
        program = TokenizeBaseTypes_(program);
        TimingStep();

        Step("Tokenizing constant keyword values...");
        program = TokenizeConstantKeywordValues(program);
        TimingStep();

        Step("Tokenizing generics...");
        program = TokenizeGenerics(program);
        TimingStep();

        Step("Tokenizing types_...");
        program = TokenizeTypes_(program);
        TimingStep();

        Step("Tokenizing var declarations...");
        program = TokenizeVarDeclarations(program);
        TimingStep();

        Step("Objectifying structs...");
        program = ObjectifyingStructs(program);
        TimingStep();

        Step("Converting template arguments...");
        program = ConvertTemplateArguments(program);
        TimingStep();

        Step("Parsing templates...");
        program = ParseFunctionTemplates(program);
        TimingStep();

        Step("Parsing function signatures...");
        program = ParseFunctionSignatures(program);
        TimingStep();

        Step("Objectifying functions...");
        program = ObjectifyingFunctions(program);
        TimingStep();

        Step("Splitting program blocks into lines...");
        program = SplitProgramBlocksIntoLines(program);
        TimingStep();

        Step("Converting string literals...");
        program = ConvertStringLiterals(program);
        TimingStep();

        Step("Tokenizing groups...");
        program = TokenizeGroups(program);
        TimingStep();

        Step("Tokenizing for loops...");
        program = TokenizeForLoops(program);
        TimingStep();

        Step("Getting scope variables...");
        program = GetScopeVariables(program);
        TimingStep();

        Step("Parsing function code...");
        program = ParseFunctionCode(program);
        TimingStep();

        if (PRINT_AST) Console.WriteLine(program);

        Step("Verifying code...");
        VerifyCode(program);
        TimingStep();

        Step("Adding unused value wrappers...");
        AddUnusedValueWrappers(program);
        TimingStep();

        Step("Getting JSON...");
        string json = GetJSON(program);
        TimingStep();

        Step("Saving JSON...");
        SaveJSON(json);
        TimingStep();

        Step("Creating LLVM IR...");
        CreateLLVMIR();
        TimingStep();

        Step("Optimizing and compiling IR...");
        OptimizeAndLinkIR();
        TimingStep();
    }

    Program TokenizeStrings(Program program) {
        return (Program)PerformMatching(program, new StringMatcher(program));
    }

    Program TokenizeCharacterConstants(Program program) {
        Program program2 = (Program)PerformMatching(
            program, new PatternMatcher(new List<IPatternSegment> {
                new TextPatternSegment("'"),
                new TypePatternSegment(typeof(TextToken)),
                new TextPatternSegment("'")
            }, new FuncPatternProcessor<List<IToken>>(tokens => {
                string txt = String.Join("", tokens.ConvertAll<string>(token=>token.ToString()));
                return new List<IToken> {new ConstantValue(CharConstant.FromString(txt))};
            }))
        );
        return (Program)PerformMatching(
            program2, new PatternMatcher(new List<IPatternSegment> {
                new TextPatternSegment("'"),
                new TextPatternSegment("\\"),
                new TypePatternSegment(typeof(TextToken)),
                new TextPatternSegment("'")
            }, new FuncPatternProcessor<List<IToken>>(tokens => {
                string txt = String.Join("", tokens.ConvertAll<string>(token=>token.ToString()));
                return new List<IToken> {new ConstantValue(CharConstant.FromString(txt))};
            }))
        );
    }

    Program RemoveComments(Program program) {
        List<IToken> tokens = new List<IToken>();
        bool wasSlash = false;
        bool wasStar = false;
        bool isLineComment = false;
        bool isBlockComment = false;
        foreach (IToken token in program) {
            TextToken textT = token as TextToken;
            if (textT == null) {
                if (!isLineComment && !isBlockComment) tokens.Add(token);
            } else {
                string text = textT.GetText();
                if (isLineComment) {
                    if (text == "\n") isLineComment = false;
                } else if (isBlockComment) {
                    if (text == "*") {
                        wasStar = true;
                    }
                    if (wasStar && text == "/") {
                        isBlockComment = false;
                    }
                } else {
                    if (text == "/") {
                        if (wasSlash) {
                            isLineComment = true;
                            tokens.RemoveAt(tokens.Count-1);
                        } else {
                            wasSlash = true;
                            tokens.Add(token);
                        }
                    } else if (text == "*") {
                        if (wasSlash) {
                            isBlockComment = true;
                            tokens.RemoveAt(tokens.Count-1);
                        } else {
                            tokens.Add(token);
                        }
                    } else {
                        tokens.Add(token);
                    }
                }
                if (text != "/") wasSlash = false;
                if (text != "*") wasStar = false;
            }
        }
        return (Program)program.Copy(tokens);
    }

    Program TokenizeFuncSignatures(Program program) {
        return (Program)PerformMatching(program, new RawFuncSignatureMatcher());
    }

    Program TokenizeNames(Program program) {
        IMatcher matcher = new NameMatcher();
        foreach (IToken token in program) {
            if (!(token is RawFuncSignature)) continue;
            RawFuncSignature sig = ((RawFuncSignature)token);
            sig.SetReturnType_(PerformMatching((TreeToken)sig.GetReturnType_(), matcher));
            sig.SetTemplate(PerformMatching((TreeToken)sig.GetTemplate(), matcher));
        }
        return (Program)PerformMatching(program, matcher);
    }

    Program TokenizeKeywords(Program program) {
        Dictionary<string, Type> keywords = new Dictionary<string, Type> {
            {"return", typeof(ReturnKeyword)},
            {"if", typeof(IfKeyword)},
            {"else", typeof(ElseKeyword)},
            {"elif", typeof(ElseIfKeyword)},
            {"while", typeof(WhileKeyword)},
            {"switch", typeof(SwitchKeyword)},
            {"break", typeof(BreakKeyword)},
            {"continue", typeof(ContinueKeyword)},
            {"null", typeof(NullValue)},
            {"for", typeof(ForKeyword)},
        };
        return (Program)PerformMatching(
            program,
            new PatternMatcher(
                new List<IPatternSegment> {
                    new UnitsPatternSegment<string>(
                        typeof(Name), keywords.Keys.ToList()
                    )
                }, new FuncPatternProcessor<List<IToken>>((List<IToken> tokens) => {
                    string name = ((Name)(tokens[0])).GetValue();
                    return new List<IToken> {(IToken)Activator.CreateInstance(
                        keywords[name]
                    )};
                })
            )
        );
    }

    Program TokenizeNumbers(Program program) {
        return (Program)PerformMatching(program, new NumberMatcher(program));
    }

    List<IToken> RemoveWhiteSpaceFilter(List<IToken> tokens) {
        return tokens.Where(
            token => {
                if (token is TextToken) {
                    TextToken text = ((TextToken)token);
                    return !Utils.Whitespace.Contains(text.GetText()[0]);
                }
                return true;
            }
        ).ToList();
    }

    Program RemoveWhitespace(Program program) {
        foreach (IToken token in program) {
            if (!(token is RawFuncSignature)) continue;
            RawFuncSignature sig = ((RawFuncSignature)token);
            RawFuncReturnType_ ret = (RawFuncReturnType_)sig.GetReturnType_();
            sig.SetReturnType_(
                (RawFuncReturnType_)ret.Copy(RemoveWhiteSpaceFilter(
                    ret.GetTokens()
                ))
            );
            RawFuncTemplate template = (RawFuncTemplate)sig.GetTemplate();
            sig.SetTemplate(
                (RawFuncTemplate)template.Copy(RemoveWhiteSpaceFilter(
                    template.GetTokens()
                ))
            );
        }
        return (Program)program.Copy(RemoveWhiteSpaceFilter(program.GetTokens()));
    }

    Program TokenizeBlocks(Program program) {
        return (Program)PerformTreeMatching(program, new BlockMatcher(
            new TextPatternSegment("{"), new TextPatternSegment("}"),
            typeof(Block)
        ));
    }

    Program TokenizeFuncArguments(Program program) {
        IMatcher matcher = new FunctionArgumentMatcher();
        for (int i = 0; i < program.Count; i++) {
            IToken token = program[i];
            if (!(token is RawFuncSignature)) continue;
            RawFuncSignature sig = ((RawFuncSignature)token);
            RawFuncTemplate template = (RawFuncTemplate)sig.GetTemplate();
            sig.SetTemplate(PerformMatching(template, matcher));
        }
        return program;
    }

    Program TokenizeFunctionHolders(Program program) {
        return (Program)PerformMatching(program, new PatternMatcher(
            new List<IPatternSegment> {
                new TypePatternSegment(typeof(RawFuncSignature)),
                new TypePatternSegment(typeof(Block))
            }, new WrapperPatternProcessor(typeof(FunctionHolder))
        ));
    }

    Program TokenizeStructHolders(Program program) {
        return (Program)PerformMatching(program, new PatternMatcher(
            new List<IPatternSegment> {
                new TypePatternSegment(typeof(Name)),
                new TypePatternSegment(typeof(Block))
            }, new WrapperPatternProcessor(typeof(StructHolder))
        ));
    }

    Program ConvertFunctionBlocks(Program program) {
        IMatcher matcher = new PatternMatcher(
            new List<IPatternSegment> {
                new TypePatternSegment(typeof(Block), true)
            }, new WrapperPatternProcessor(
                new UnwrapperPatternProcessor(),
                typeof(CodeBlock)
            )
        );
        for (int i = 0; i < program.Count; i++) {
            IToken token = program[i];
            if (token is FunctionHolder) {
                FunctionHolder holder = ((FunctionHolder)token);
                program[i] = PerformTreeMatching(holder, matcher);
            }
        }
        return program;
    }

    void ComputeBaseTypes_(Program program) {
        List<string> types_ = new List<string>();
        foreach (IToken token_ in program) {
            if (token_ is StructHolder) {
                StructHolder token = ((StructHolder)token_);
                if (token.Count == 0) continue;
                IToken name = token[0];
                if (name is Name) {
                    types_.Add(((Name)name).GetValue());
                }
            }
        }
        program.SetBaseType_Names(types_);
    }

    Program TokenizeBaseTypes_(Program program) {
        List<string> baseType_Names = program.GetBaseType_Names();
        Func<string, UserBaseType_> converter = (string source) => 
            UserBaseType_.ParseString(source, baseType_Names);
        IMatcher matcher = new UnitSwitcherMatcher<string, UserBaseType_>(
            typeof(Name), converter, typeof(UserBaseType_Token)
        );
        foreach (IToken token in program) {
            if (token is Holder) {
                Holder holder = ((Holder)token);
                Block block = holder.GetBlock();
                if (block == null) continue;
                TreeToken result = PerformTreeMatching(block, matcher);
                holder.SetBlock((Block)result);
                if (token is FunctionHolder) {
                    RawFuncSignature sig = ((FunctionHolder)token).GetRawSignature();
                    sig.SetReturnType_(PerformTreeMatching(
                        (TreeToken)sig.GetReturnType_(), matcher)
                    );
                    TreeToken template = (TreeToken)sig.GetTemplate();
                    for (int i = 0; i < template.Count; i++) {
                        IToken sub = template[i];
                        if (sub is RawFunctionArgument) {
                            template[i] = PerformTreeMatching((RawFunctionArgument)sub, matcher);
                        }
                    }
                }
            }
        }
        return program;
    }

    Program TokenizeConstantKeywordValues(Program program) {
        Dictionary<string, Func<IConstant>> constantValues = new Dictionary<string, Func<IConstant>> {
            {"true", () => new BoolConstant(true)},
            {"false", () => new BoolConstant(false)},
            {"infinity", () => new FloatConstant(Double.NegativeInfinity)},
            {"NaN", () => new FloatConstant(Double.NaN)},
            {"pi", () => new FloatConstant(MathF.PI)},
        };
        IMatcher matcher = new PatternMatcher(
            new List<IPatternSegment> {
                new UnitsPatternSegment<string>(
                    typeof(Name), constantValues.Keys.ToList()
                )
            }, new FuncPatternProcessor<List<IToken>>((List<IToken> tokens) => {
                string name = ((Name)tokens[0]).GetValue();
                return new List<IToken> {
                    new ConstantValue(constantValues[name]())
                };
            })
        );
        foreach (IToken token in program) {
            if (token is FunctionHolder) {
                FunctionHolder holder = ((FunctionHolder)token);
                Block block = holder.GetBlock();
                if (block == null) continue;
                TreeToken result = PerformTreeMatching(block, matcher);
                holder.SetBlock((Block)result);
            }
        }
        return program;
    }

    Program TokenizeGenerics(Program program) {
        IMatcher matcher = new BlockMatcher(
            new TypePatternSegment(typeof(UserBaseType_Token)),
            new TextPatternSegment("<"), new TextPatternSegment(">"),
            typeof(Generics)
        );
        for (int i = 0; i < program.Count; i++) {
            IToken token = program[i];
            if (!(token is FunctionHolder)) continue;
            RawFuncSignature sig = ((FunctionHolder)token).GetRawSignature();
            sig.SetReturnType_(
                PerformTreeMatching((TreeToken)sig.GetReturnType_(), matcher)
            );
            sig.SetTemplate(
                PerformTreeMatching((TreeToken)sig.GetTemplate(), matcher)
            );
        }
        return (Program)PerformTreeMatching(program, matcher);
    }

    Program TokenizeTypes_(Program program) {
        IMatcher matcher = new CombinedMatchersMatcher(new List<IMatcher> {
            new PatternMatcher(
                new List<IPatternSegment> {
                    new TextPatternSegment("["),
                    new TypePatternSegment(typeof(Type_Token)),
                    new TextPatternSegment("]")
                }, new FuncPatternProcessor<List<IToken>>(
                    tokens => new List<IToken> {
                        new Type_Token(new Type_("Array", new List<Type_> {
                            ((Type_Token)tokens[1]).GetValue()
                        }))
                    }
                )
            ),
            new PatternMatcher(
                new List<IPatternSegment> {
                    new TypePatternSegment(typeof(Type_Token)),
                    new TextPatternSegment("?")
                }, new FuncPatternProcessor<List<IToken>>(
                    tokens => new List<IToken> {
                        new Type_Token(new Type_("Optional", new List<Type_> {
                            ((Type_Token)tokens[0]).GetValue()
                        }))
                    }
                )
            ),
            new Type_Matcher(),
        });
        foreach (IToken token in program) {
            if (token is Holder) {
                Holder holder = ((Holder)token);
                Block block = holder.GetBlock();
                if (block == null) continue;
                TreeToken result = PerformTreeMatching(block, matcher);
                holder.SetBlock((Block)result);
                if (token is FunctionHolder) {
                    RawFuncSignature sig = ((FunctionHolder)token).GetRawSignature();
                    sig.SetReturnType_(PerformTreeMatching(
                        (TreeToken)sig.GetReturnType_(), matcher
                    ));
                    TreeToken template = (TreeToken)sig.GetTemplate();
                    for (int i = 0; i < template.Count; i++) {
                        IToken sub = template[i];
                        if (sub is RawFunctionArgument) {
                            template[i] = PerformTreeMatching(
                                (RawFunctionArgument)sub, matcher
                            );
                        }
                    }
                }
            }
        }
        return program;
    }

    Program TokenizeVarDeclarations(Program program) {
        foreach (IToken token in program) {
            if (token is Holder) {
                Holder holder = ((Holder)token);
                Block block = holder.GetBlock();
                if (block == null) continue;
                TreeToken result = PerformTreeMatching(block,
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new TypePatternSegment(typeof(Type_Token)),
                            new TextPatternSegment(":"),
                            new TypePatternSegment(typeof(Name))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(VarDeclaration)
                        )
                    )
                );
                holder.SetBlock((Block)result);
            }
        }
        return program;
    }

    Program ObjectifyingStructs(Program program) {
        return (Program)PerformMatching(program, new StructObjectifyerMatcher());
    }

    Program ConvertTemplateArguments(Program program) {
        List<IPatternSegment> functionArgumentSegments = new List<IPatternSegment> {
            new TypePatternSegment(typeof(Type_Token)),
            new TextPatternSegment(":"),
            new TypePatternSegment(typeof(Name))
        };
        IMatcher argumentConverterMatcher = new PatternMatcher(
            new List<IPatternSegment> {
                new FuncPatternSegment<RawFunctionArgument>(arg => {
                    if (!TokenUtils.FullMatch(functionArgumentSegments, arg.GetTokens())) {
                        throw new SyntaxErrorException(
                            "Malformed function argument", arg
                        );
                    }
                    return true;
                })
            }, new FuncPatternProcessor<List<IToken>>(tokens => {
                RawFunctionArgument arg = (RawFunctionArgument)tokens[0];
                return new List<IToken> {
                    new FunctionArgumentToken(
                        ((Name)arg[2]).GetValue(), ((Type_Token)arg[0]).GetValue()
                    )
                };
            })
        );
        for (int i = 0; i < program.Count; i++) {
            IToken token = program[i];
            if (!(token is FunctionHolder)) continue;
            FunctionHolder holder = ((FunctionHolder)token);
            RawFuncSignature sig = holder.GetRawSignature();
            TreeToken template = (TreeToken)sig.GetTemplate();
            template = PerformMatching(template, argumentConverterMatcher);
            sig.SetTemplate(template);
        }
        return program;
    }

    Program ParseFunctionTemplates(Program program) {
        for (int i = 0; i < program.Count; i++) {
            IToken token = program[i];
            if (!(token is FunctionHolder)) continue;
            FunctionHolder holder = ((FunctionHolder)token);
            RawFuncSignature sig = holder.GetRawSignature();
            RawFuncTemplate rawTemplate = (RawFuncTemplate)sig.GetTemplate();
            List<IPatternSegment> segments = new List<IPatternSegment>();
            List<FunctionArgumentToken> arguments = new List<FunctionArgumentToken>();
            List<int> slots = new List<int>();
            int j = -1;
            foreach (IToken subtoken in rawTemplate) {
                j++;
                Type tokenType = subtoken.GetType();
                IPatternSegment segment = null;
                if (subtoken is TextToken) {
                    segment = new TextPatternSegment(
                        ((TextToken)subtoken).GetText()
                    );
                } else if (subtoken is Unit<string>) {
                    segment = new UnitPatternSegment<string>(
                        tokenType, ((Unit<string>)subtoken).GetValue()
                    );
                } else if (subtoken is FunctionArgumentToken) {
                    FunctionArgumentToken argument = ((FunctionArgumentToken)subtoken);
                    arguments.Add(argument);
                    segment = new TypePatternSegment(typeof(RawSquareGroup));
                    slots.Add(j);
                }
                if (segment == null) {
                    throw new SyntaxErrorException(
                        "Invalid syntax in function template", subtoken
                    );
                }
                segments.Add(segment);
            }
            sig.SetTemplate(new FuncTemplate(
                new ConfigurablePatternExtractor<List<IToken>>(
                    segments, new SlotPatternProcessor(slots)
                ),
                arguments
            ));
        }
        return program;
    }

    Program ParseFunctionSignatures(Program program) {
        for (int i = 0; i < program.Count; i++) {
            IToken token = program[i];
            if (!(token is FunctionHolder)) continue;
            FunctionHolder holder = ((FunctionHolder)token);
            RawFuncSignature sig = holder.GetRawSignature();
            holder.SetSignature(
                new FuncSignature(
                    ((RawFuncReturnType_)sig.GetReturnType_()).GetType_(),
                    (FuncTemplate)sig.GetTemplate()
                )
            );
        }
        return program;
    }

    Program ObjectifyingFunctions(Program program) {
        return (Program)PerformMatching(program, new PatternMatcher(
            new List<IPatternSegment> {
                new TypePatternSegment(typeof(FunctionHolder))
            }, new FuncPatternProcessor<List<IToken>>((List<IToken> tokens) => {
                FunctionHolder holder = ((FunctionHolder)tokens[0]);
                FuncSignature sig = holder.GetSignature();
                FuncTemplate template = sig.GetTemplate();
                return new List<IToken> {
                    new Function(
                        template.GetValue(), template.GetArguments(),
                        (CodeBlock)holder.GetBlock(), sig.GetReturnType_()
                    )
                };
            })
        ));
    }

    Program SplitProgramBlocksIntoLines(Program program) {
        foreach (IToken token in TokenUtils.Traverse(program)) {
            if (token is IParentToken) {
                IParentToken parent = ((IParentToken)token);
                for (int i = 0; i < parent.Count; i++) {
                    IToken sub = parent[i];
                    if (sub is CodeBlock) {
                        parent[i] = SplitBlockIntoLines((CodeBlock)sub);
                    }
                }
            }
        }
        return program;
    }

    CodeBlock SplitBlockIntoLines(CodeBlock block) {
        SplitTokensParser parser = new SplitTokensParser(
            new TextPatternSegment(";"), false
        );
        List<List<IToken>> rawLines = parser.Parse(block);
        if (rawLines == null) {
            throw new SyntaxErrorException("Missing semicolon", block);
        }
        List<IToken> lines = new List<IToken>();
        foreach(List<IToken> section in rawLines) {
            Line line = new Line(section);
            line.span = TokenUtils.MergeSpans(section);
            lines.Add(line);
        }
        return (CodeBlock)block.Copy(lines);
    }

    Program ConvertStringLiterals(Program program) {
        return (Program)PerformTreeMatching(
            program, new PatternMatcher(
                new List<IPatternSegment> {
                    new FuncPatternSegment<ConstantValue>(
                        constant => constant.GetValue() is StringConstant
                    )
                }, new FuncPatternProcessor<List<IToken>>(tokens => {
                    ConstantValue sval = (ConstantValue)tokens[0];
                    string text = (((StringConstant)sval.GetValue()).GetValue());
                    return new List<IToken> {new StringLiteral(text)};
                })
            )
        );
    }

    Program TokenizeGroups(Program _program) {
        List<IMatcher> matchers = new List<IMatcher> {
            new PatternMatcher(
                new List<IPatternSegment> {
                    new TextPatternSegment("("),
                    new TypePatternSegment(typeof(Type_Token)),
                    new TextPatternSegment(")")
                }, new Wrapper2PatternProcessor(
                    new SlotPatternProcessor(new List<int> {1}),
                    typeof(UnmatchedCast)
                )
            ),
            new BlockMatcher(
                new TextPatternSegment("["), new TextPatternSegment("]"),
                typeof(RawSquareGroup)
            ),
            new BlockMatcher(
                new TextPatternSegment("("), new TextPatternSegment(")"),
                typeof(RawGroup)
            )
        };
        Program program = _program;
        foreach(IMatcher matcher in matchers) {
            program = (Program)PerformTreeMatching(program, matcher);
        }
        return program;
    }

    Program TokenizeForLoops(Program program) {
        return (Program)PerformTreeMatching(
            program, new PatternMatcher(
                new List<IPatternSegment> {
                    new TypePatternSegment(typeof(ForKeyword)),
                    new TypePatternSegment(typeof(RawGroup)),
                    new TypePatternSegment(typeof(CodeBlock))
                }, new FuncPatternProcessor<List<IToken>>(tokens => {
                    List<IToken> condition = ((RawGroup)tokens[1]).GetTokens();
                    if (condition.Count <= 1) {
                        throw new SyntaxErrorException(
                            "For loop condition cannot be empty and must have at least one clause", tokens[1]
                        );
                    }
                    return new List<IToken> {new RawFor(
                        condition, (CodeBlock)tokens[2]
                    )};
                })
            )
        );
    }

    Program GetScopeVariables(Program program) {
        program.UpdateParents();
        
        foreach (IToken token in program) {
            if (token is Function) {
                Function function = ((Function)token);
                foreach (VarDeclaration declaration 
                        in TokenUtils.TraverseFind<VarDeclaration>(function)) {
                    string name = declaration.GetName().GetValue();
                    Scope scope = Scope.GetEnclosing(declaration);
                    declaration.SetID(scope.AddVar(
                        name, declaration.GetType_()
                    ));
                }
            }
        }
        
        return program;
    }

    Program ParseFunctionCode(Program program) {
        List<IMatcher> functionRules = new List<IMatcher>();
        List<IMatcher> addMatchingFunctionRules = new List<IMatcher>();

        List<FunctionDeclaration> functions = new List<FunctionDeclaration>();
        functions.AddRange(BuiltinsList.Builtins);

        foreach (IToken token in program) {
            if (token is Function) {
                functions.Add((Function)token);
            }
        }

        functions.Sort();

        List<PatternExtractor<List<IToken>>> extractors = new List<PatternExtractor<List<IToken>>>();
        foreach (FunctionDeclaration function in functions) {
            PatternExtractor<List<IToken>> extractor = function.GetPattern();
            bool unique = true;
            foreach (PatternExtractor<List<IToken>> oextractor in extractors) {
                if (oextractor.Equals(extractor)) unique = false;
            }
            if (unique) {
                extractors.Add(extractor);
                functionRules.Add(new FunctionRuleMatcher(extractor));
            }
        }

        foreach (FunctionDeclaration function in functions) {
            addMatchingFunctionRules.Add(
                new AddMatchingFunctionMatcher(function)
            );
        }

        Dictionary<string, Type> floatCompoundableOperators = new Dictionary<string, Type> {
            {"+", typeof(Addition)}, {"-", typeof(Subtraction)}, 
            {"*", typeof(Multiplication)}, {"/", typeof(Division)},
            {"%", typeof(Modulo)}
        };

        Dictionary<string, Type> intCompoundableOperators = new Dictionary<string, Type> {
            {"&", typeof(BitwiseAND)}, {"|", typeof(BitwiseOR)},
            {"^", typeof(BitwiseXOR)}
        };

        List<List<IMatcher>> rules = new List<List<IMatcher>> {
            functionRules,
            addMatchingFunctionRules,
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new FuncPatternSegment<Name>(
                            (Name name) => Scope.GetEnclosing(name)
                                                .ContainsVar(name.GetValue())
                        ),
                    }, new Wrapper2PatternProcessor(
                        typeof(Variable)
                    )
                )
            },
            new List<IMatcher> {
                new GroupConverterMatcher(typeof(RawGroup), typeof(Group)),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(RawFunctionCall))
                    }, new FuncPatternProcessor<List<IToken>>((List<IToken> tokens) => {
                        RawFunctionCall call = ((RawFunctionCall)(tokens[0]));

                        List<Type_> paramTypes_ = new List<Type_>();
                        List<IValueToken> parameters = new List<IValueToken>();

                        for (int i = 0; i < call.Count; i++) {
                            RawSquareGroup rparameter = (call[i]) as RawSquareGroup;
                            if (rparameter.Count != 1) {
                                throw new SyntaxErrorException(
                                    "Function parameters cannot be empty", rparameter
                                );
                            }
                            IValueToken parameter = (rparameter[0]) as IValueToken;
                            if (parameter == null) {
                                throw new SyntaxErrorException(
                                    "Illegal syntax in function parameter", rparameter
                                );
                            }
                            paramTypes_.Add(parameter.GetType_());
                            parameters.Add(parameter);
                        }

                        foreach (FunctionDeclaration function in call.GetMatchingFunctions()) {
                            List<Type_> argTypes_ = function.GetArguments().ConvertAll<Type_>(
                                (FunctionArgument arg) => arg.GetType_()
                            );
                            if (paramTypes_.Count != argTypes_.Count) continue;

                            bool matches = true;

                            for (int i = 0; i < paramTypes_.Count; i++) {
                                Type_ pt = paramTypes_[i];
                                Type_ at = argTypes_[i];
                                if (!pt.IsConvertibleTo(at)) {
                                    matches = false;
                                    break;
                                }
                            }

                            if (matches) {
                                return new List<IToken> {
                                    new FunctionCall(
                                        function, parameters
                                    )
                                };
                            }
                        }

                        return null;
                    })
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new FuncPatternSegment<RawSquareGroup>(
                            (RawSquareGroup group) => !(group.parent is RawFunctionCall)
                        )
                    }, new WrapperPatternProcessor(
                        new SplitTokensProcessor(
                            new UnwrapperPatternProcessor(),
                            new TextPatternSegment(","),
                            typeof(ValueListItem)
                        ),
                        typeof(ValueList)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(Type_Token)),
                        new TypePatternSegment(typeof(ValueList))
                    }, new Wrapper2PatternProcessor(
                        typeof(Instantiation)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new FuncPatternSegment<Instantiation>(
                            i => i.GetType_().GetBaseType_().GetName() == "Array"
                        )
                    }, new Wrapper2PatternProcessor(
                        typeof(ArrayCreation)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new Type_PatternSegment(new Type_("Array", new List<Type_> {Type_.Any()})),
                        new TypePatternSegment(typeof(ValueList))
                    }, new Wrapper2PatternProcessor(
                        typeof(ArrayAccess)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new Type_PatternSegment(Type_.Any()),
                        new TextPatternSegment("."),
                        new TypePatternSegment(typeof(Name))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {0, 2}),
                        typeof(MemberAccess)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(BreakKeyword))
                    }, new InstantiationPatternProcessor(typeof(Break))
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(ContinueKeyword))
                    }, new InstantiationPatternProcessor(typeof(Continue))
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(IfKeyword)),
                        new TypePatternSegment(typeof(Group)),
                        new TypePatternSegment(typeof(CodeBlock))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {1, 2}),
                        typeof(Conditional)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(Conditional)),
                        new TypePatternSegment(typeof(ElseIfKeyword)),
                        new TypePatternSegment(typeof(Group)),
                        new TypePatternSegment(typeof(CodeBlock))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {0, 2, 3}),
                        typeof(Conditional)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(Conditional)),
                        new TypePatternSegment(typeof(ElseKeyword)),
                        new TypePatternSegment(typeof(CodeBlock))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {0, 2}),
                        typeof(Conditional)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(RawFor))
                    }, new Wrapper2PatternProcessor(
                        typeof(For)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(WhileKeyword)),
                        new TypePatternSegment(typeof(Group)),
                        new TypePatternSegment(typeof(CodeBlock))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {1, 2}),
                        typeof(While)
                    )
                ),
                new AdvancedPatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(SwitchKeyword)),
                        new TypePatternSegment(typeof(Group))
                    }, new List<IPatternSegment> {
                        new TypePatternSegment(typeof(Group)),
                        new TypePatternSegment(typeof(CodeBlock))
                    }, 1, -1, new List<IPatternSegment> {
                        new TypePatternSegment(typeof(CodeBlock))
                    }, 
                    new FuncPatternProcessor<List<IToken>>((List<IToken> tokens) => {
                        return new List<IToken> {
                            new Switch((IValueToken)tokens[1], tokens.Skip(2).ToArray())
                        };
                    })
                ),
                new AdvancedPatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(SwitchKeyword)),
                        new TypePatternSegment(typeof(Group))
                    }, new List<IPatternSegment> {
                        new TypePatternSegment(typeof(Group)),
                        new TypePatternSegment(typeof(CodeBlock))
                    }, 1, -1, new List<IPatternSegment>(), 
                    new FuncPatternProcessor<List<IToken>>((List<IToken> tokens) => {
                        return new List<IToken> {
                            new Switch((IValueToken)tokens[1], tokens.Skip(2).ToArray())
                        };
                    })
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TextPatternSegment("!"),
                        new Type_PatternSegment(Type_.Any())
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {1}),
                        typeof(Not)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TextPatternSegment("~"),
                        new Type_PatternSegment(new Type_("Z"))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {1}),
                        typeof(BitwiseNOT)
                    )
                ),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new AndPatternSegment(
                                new Type_PatternSegment(new Type_("Q")),
                                new TypePatternSegment(typeof(IAssignableValue))
                            ),
                            new TextPatternSegment("+"),
                            new TextPatternSegment("+")
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0}),
                            typeof(PostIncrement)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new AndPatternSegment(
                                new Type_PatternSegment(new Type_("Q")),
                                new TypePatternSegment(typeof(IAssignableValue))
                            ),
                            new TextPatternSegment("-"),
                            new TextPatternSegment("-")
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0}),
                            typeof(PostDecrement)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new TextPatternSegment("+"),
                            new TextPatternSegment("+"),
                            new AndPatternSegment(
                                new Type_PatternSegment(new Type_("Q")),
                                new TypePatternSegment(typeof(IAssignableValue))
                            )
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {2}),
                            typeof(PreIncrement)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new TextPatternSegment("-"),
                            new TextPatternSegment("-"),
                            new AndPatternSegment(
                                new Type_PatternSegment(new Type_("Q")),
                                new TypePatternSegment(typeof(IAssignableValue))
                            )
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {2}),
                            typeof(PreDecrement)
                        )
                    )
                }),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new Type_PatternSegment(new Type_("Q")),
                        new TextPatternSegment("*"),
                        new TextPatternSegment("*"),
                        new Type_PatternSegment(new Type_("Q"))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {0, 3}),
                        typeof(Exponentiation)
                    )
                ),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TextPatternSegment("-"),
                        new Type_PatternSegment(new Type_("Q"))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {1}),
                        typeof(Negation)
                    )
                ),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("*"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(Multiplication)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("/"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}), 
                            typeof(Division)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Z")),
                            new TextPatternSegment("~"),
                            new TextPatternSegment("/"),
                            new Type_PatternSegment(new Type_("Z"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}), 
                            typeof(IntDivision)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("%"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(Modulo)
                        )
                    ),
                }),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("+"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(Addition)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("-"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(Subtraction)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TypePatternSegment(typeof(Negation))
                        }, new FuncPatternProcessor<List<IToken>>(tokens => new List<IToken> {
                            new Subtraction(
                                (IValueToken)tokens[0], ((Negation)tokens[1]).Sub()
                            )
                        })
                    ),
                }),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Z")),
                            new TextPatternSegment(">"),
                            new TextPatternSegment(">"),
                            new Type_PatternSegment(new Type_("Z"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(BitshiftRight)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Z")),
                            new TextPatternSegment("<"),
                            new TextPatternSegment("<"),
                            new Type_PatternSegment(new Type_("Z"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(BitshiftLeft)
                        )
                    ),
                }),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Z")),
                            new TextPatternSegment("&"),
                            new Type_PatternSegment(new Type_("Z"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(BitwiseAND)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Z")),
                            new TextPatternSegment("|"),
                            new Type_PatternSegment(new Type_("Z"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(BitwiseOR)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Z")),
                            new TextPatternSegment("^"),
                            new Type_PatternSegment(new Type_("Z"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(BitwiseXOR)
                        )
                    ),
                }),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment(">"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(Greater)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("<"),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 2}),
                            typeof(Less)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment(">"),
                            new TextPatternSegment("="),
                            new Type_PatternSegment(new Type_("Q"))
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(GreaterEqual)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(new Type_("Q")),
                            new TextPatternSegment("<"),
                            new TextPatternSegment("="),
                            new Type_PatternSegment(new Type_("Q")),
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(LessEqual)
                        )
                    ),
                }),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(Type_.Any()),
                            new TextPatternSegment("="),
                            new TextPatternSegment("="),
                            new Type_PatternSegment(Type_.Any())
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(Equals)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(Type_.Any()),
                            new TextPatternSegment("!"),
                            new TextPatternSegment("="),
                            new Type_PatternSegment(Type_.Any())
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(NotEquals)
                        )
                    ),
                }),
                new CombinedMatchersMatcher(new List<IMatcher> {
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(Type_.Any()),
                            new TextPatternSegment("&"),
                            new TextPatternSegment("&"),
                            new Type_PatternSegment(Type_.Any())
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(And)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(Type_.Any()),
                            new TextPatternSegment("|"),
                            new TextPatternSegment("|"),
                            new Type_PatternSegment(Type_.Any())
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(Or)
                        )
                    ),
                    new PatternMatcher(
                        new List<IPatternSegment> {
                            new Type_PatternSegment(Type_.Any()),
                            new TextPatternSegment("^"),
                            new TextPatternSegment("^"),
                            new Type_PatternSegment(Type_.Any())
                        }, new Wrapper2PatternProcessor(
                            new SlotPatternProcessor(new List<int> {0, 3}),
                            typeof(Xor)
                        )
                    ),
                }),
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(UnmatchedCast)),
                        new Type_PatternSegment(Type_.Any())
                    }, new Wrapper2PatternProcessor(typeof(Cast))
                ),
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(VarDeclaration)),
                        new TextPatternSegment("="),
                        new Type_PatternSegment(Type_.Any())
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {0, 2}),
                        typeof(InitialAssignment)
                    )
                )
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(VarDeclaration))
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {0}),
                        typeof(UninitVarDeclaration)
                    )
                )
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(IAssignableValue)),
                        new TextPatternSegment("="),
                        new Type_PatternSegment(Type_.Any())
                    }, new FuncPatternProcessor<List<IToken>>(tokens => new List<IToken> {
                        ((IAssignableValue)tokens[0]).AssignTo(
                            (IValueToken)tokens[2]
                        )
                    })
                )
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(IAssignableValue)),
                        new TextsPatternSegment(
                            floatCompoundableOperators.Keys.ToList()
                        ),
                        new TextPatternSegment("="),
                        new Type_PatternSegment(new Type_("Q"))
                    }, new FuncPatternProcessor<List<IToken>>(tokens => new List<IToken> {
                        new CompoundAssignment(
                            floatCompoundableOperators[
                                ((TextToken)tokens[1]).GetText()
                            ], (IAssignableValue)tokens[0], (IValueToken)tokens[3]
                        )
                    })
                )
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(IAssignableValue)),
                        new TextsPatternSegment(
                            intCompoundableOperators.Keys.ToList()
                        ),
                        new TextPatternSegment("="),
                        new Type_PatternSegment(new Type_("Z"))
                    }, new FuncPatternProcessor<List<IToken>>(tokens => new List<IToken> {
                        new CompoundAssignment(
                            intCompoundableOperators[
                                ((TextToken)tokens[1]).GetText()
                            ], (IAssignableValue)tokens[0], (IValueToken)tokens[3]
                        )
                    })
                )
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new FuncPatternSegment<Division>(division => (
                            (division[0] is ConstantValue)
                            && (division[1] is ConstantValue)
                            && (((ConstantValue)division[0]).GetValue() is INumberConstant)
                            && (((ConstantValue)division[1]).GetValue() is INumberConstant)
                        ))
                    }, new FuncPatternProcessor<List<IToken>>(tokens => new List<IToken> {
                        new ConstantValue(
                            new FloatConstant(
                                (
                                    ((INumberConstant)
                                        ((ConstantValue)
                                            ((Division)tokens[0])[0]
                                        ).GetValue()
                                    ).GetDoubleValue()
                                ) / (
                                    ((INumberConstant)
                                        ((ConstantValue)
                                            ((Division)tokens[0])[1]
                                        ).GetValue()
                                    ).GetDoubleValue()
                                )
                            )
                        )
                    })
                )
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(ReturnKeyword)),
                        new Type_PatternSegment(Type_.Any())
                    }, new Wrapper2PatternProcessor(
                        new SlotPatternProcessor(new List<int> {1}),
                        typeof(Return)
                    )
                ),
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(ReturnKeyword))
                    }, new InstantiationPatternProcessor(
                        typeof(ReturnVoid)
                    )
                ),
            },
            new List<IMatcher> {
                new PatternMatcher(
                    new List<IPatternSegment> {
                        new TypePatternSegment(typeof(Group)),
                    }, new UnwrapperPatternProcessor()
                ),
            },
        };

        foreach (CodeBlock block in TokenUtils.TraverseFind<CodeBlock>(program)) {
            DoBlockCodeRules(block, rules);
        }

        return program;
    }

    void DoBlockCodeRules(CodeBlock block, List<List<IMatcher>> rules) {
        for (int i = 0; i < block.Count; i++) {
            Line line = block[i] as Line;
            if (line == null) continue;
            foreach (List<IMatcher> ruleset in rules) {
                line = (Line)DoTreeCodeRules(line, ruleset);
            }
            block[i] = line;
        }
    }

    IParentToken DoTreeCodeRules(IParentToken parent_, List<IMatcher> ruleset) {
        IParentToken parent = parent_;
        bool changed = true;
        while (changed) {
            changed = false;
            for (int i = 0; i < parent.Count; i++) {
                IToken sub = parent[i];
                if (sub is IParentToken && !(sub is IBarMatchingInto || sub is CodeBlock)) {
                    IParentToken subparent = ((IParentToken)sub);
                    parent[i] = DoTreeCodeRules(subparent, ruleset);
                }
            }
            if (parent is TreeToken) {
                TreeToken tree = ((TreeToken)parent);
                foreach (IMatcher rule in ruleset) {
                    Match match = rule.Match(tree);
                    if (match != null) {
                        changed = true;
                        parent = match.Replace(tree);
                        TokenUtils.UpdateParents(parent);
                        break;
                    }
                }
            } else {
                foreach (IMatcher rule in ruleset) {
                    Match match = rule.Match(parent);
                    if (match != null && match.Length() == 1) {
                        changed = true;
                        match.SingleReplace(parent);
                        TokenUtils.UpdateParents(parent);
                        break;
                    }
                }
            }
        }
        return parent;
    }

    TreeToken PerformTreeMatching(TreeToken tree, IMatcher matcher) {
        (bool _, IParentToken result) = PerformTreeMatching_(tree, matcher);
        return (TreeToken)result;
    }

    (bool, IParentToken) PerformTreeMatching_(IParentToken parent, IMatcher matcher) {
        bool changed = true;
        bool anyChanged = false;

        while (changed) {
            changed = false;

            for (int i = 0; i < parent.Count; i++) {
                IToken sub = parent[i];
                if (sub is IParentToken && !(sub is IBarMatchingInto)) {
                    IParentToken subparent = ((IParentToken)sub);
                    bool tchanged;
                    (tchanged, parent[i]) = PerformTreeMatching_(subparent, matcher);
                    changed |= tchanged;
                }
            }

            if (parent is TreeToken) {
                TreeToken tree = ((TreeToken)parent);
                bool tchanged;
                (tchanged, parent) = PerformMatchingChanged(tree, matcher);
                changed |= tchanged;
            } else {
                changed |= PerformIParentMatchingChanged(parent, matcher);
            }

            anyChanged |= changed;
        }

        return (anyChanged, parent);
    }

    TreeToken PerformMatching(TreeToken tree, IMatcher matcher) {
        while (true) {
            Match match = matcher.Match(tree);
            if (match == null) break;
            tree = (TreeToken)match.Replace(tree);
        }
        return tree;
    }

    (bool, TreeToken) PerformMatchingChanged(TreeToken tree, IMatcher matcher) {
        bool changed = false;
        while (true) {
            Match match = matcher.Match(tree);
            if (match == null) break;
            changed = true;
            tree = (TreeToken)match.Replace(tree);
        }
        return (changed, tree);
    }

    bool PerformIParentMatchingChanged(IParentToken parent, IMatcher matcher) {
        bool changed = false;
        while (true) {
            Match match = matcher.Match(parent);
            if (match == null || match.Length() != 1) break;
            changed = true;
            match.SingleReplace(parent);
        }
        return changed;
    }

    void VerifyCode(Program program) {
        TraverseConfig config = new TraverseConfig(TraverseMode.DEPTH, invert: false);
        foreach (IVerifier token in TokenUtils.TraverseFind<IVerifier>(program, config)) {
            token.Verify();
        }
    }

    void AddUnusedValueWrappers(Program program) {
        foreach (CodeBlock block in TokenUtils.TraverseFind<CodeBlock>(program)) {
            for (int i = 0; i < block.Count; i++) {
                Line line = block[i] as Line;
                if (line == null) continue;
                IValueToken sub = line[0] as IValueToken;
                if (sub == null) continue;
                if (sub.GetType_().GetBaseType_().IsVoid()) continue;
                line[0] = new UnusedValueWrapper(sub);
            }
        }
    }

    string GetJSON(Program program) {
        return program.GetJSON().ToJSON();
    }

    void SaveJSON(string json) {
        using (StreamWriter file = new StreamWriter(Utils.ProjectAbsolutePath()+"code.json")) {
            file.Write(json);
        }
    }

    int RunCommand(string command) {
        // I know this isn't the right way to do this
        // (and I know it won't work on non-linux systems)
        ProcessStartInfo procStartInfo = new ProcessStartInfo(
            "/bin/bash", "-c " + Utils.EscapeStringToLiteral(command, '\'')
        );
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;

        Process proc = new Process();
        proc.StartInfo = procStartInfo;
        proc.Start();
        proc.WaitForExit();

        return proc.ExitCode;
    }

    void CreateLLVMIR() {
        System.IO.File.WriteAllText(Utils.ProjectAbsolutePath()+"/err.txt", "");
        int exitCode = RunCommand($"cd {Utils.ProjectAbsolutePath()};source venv/bin/activate;python LLVMIR/create_ir.py 2> err.txt");
        using (StreamReader file = new StreamReader(Utils.ProjectAbsolutePath()+"/err.txt")) {
            string log = file.ReadToEnd();
            if (log.Length > 0) {
                throw new PythonExceptionException(log);
            }
        }
        if (exitCode != 0)
            throw new BashExceptionException("Something went wrong with LLVMIR creation");
    }

    void OptimizeAndLinkIR() {
        System.IO.File.WriteAllText(Utils.ProjectAbsolutePath()+"/err.txt", "");
        int exitCode = RunCommand($"cd {Utils.ProjectAbsolutePath()};./linkoptimize.bash 2> err.txt");
        using (StreamReader file = new StreamReader(Utils.ProjectAbsolutePath()+"/err.txt")) {
            string log = file.ReadToEnd();
            if (log.Length > 0) {
                throw new BashExceptionException(log);
            }
        }
        if (exitCode != 0)
            throw new BashExceptionException("Something went wrong with optimization and linking creation");
    }

    public int CompileIR() {
        if (CATCH_ERRS) {
            try {
                _CompileIR();
                return 0;
            } catch (BashExceptionException e) {
                Console.WriteLine("Error in Bash code:");
                Console.WriteLine(e.Message);
                return 1;
            }
        } else {
            _CompileIR();
            return 0;
        }
    }

    void _CompileIR() {
        System.IO.File.WriteAllText(Utils.ProjectAbsolutePath()+"/err.txt", "");
        int exitCode = RunCommand($"cd {Utils.ProjectAbsolutePath()};./compileir.bash 2> err.txt");
        using (StreamReader file = new StreamReader(Utils.ProjectAbsolutePath()+"/err.txt")) {
            string log = file.ReadToEnd();
            if (log.Length > 0) {
                throw new BashExceptionException(log);
            }
        }
        if (exitCode != 0)
            throw new BashExceptionException("Something went wrong with final compilation");
    }
}
