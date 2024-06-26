using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class SPECFileCompiler : IFileCompiler {
    string path;
    string fileText;
    ShapedJSON obj;
    Dictionary<string, Type_> types_ = new Dictionary<string, Type_>();
    public static IJSONShape Shape { get => _Shape; }
    static IJSONShape _Shape;

    static SPECFileCompiler() {
        _Shape = new JSONObjectShape(new Dictionary<string, IJSONShape> {
            {"functions", new JSONListShape(new JSONObjectShape(
                new Dictionary<string, IJSONShape> {
                    {"id", new JSONStringShape()},
                    {"callee", new JSONStringShape()},
                    {"return_type_", new JSONStringShape()},
                    {"template", new JSONListShape(new JSONObjectShape(
                        new Dictionary<string, IJSONShape> {
                            {"type", new JSONStringShape()}
                        }
                    ))},
                    {"takes_ownership", new JSONBoolShape()},
                    {"result_in_params", new JSONBoolShape()},
                    {"source", new JSONStringOptionsShape(new List<string> {
                        "Program", "Library", "Builtin"
                    })}
                }
            ))},
            {"types_", new JSONListShape(new JSONObjectShape(new Dictionary<string, IJSONShape> {
                {"given_name", new JSONStringShape()},
                {"name", new JSONStringShape()},
                {"bits", new JSONNullableShape(new JSONIntShape())},
                {"generics", new JSONListShape(new JSONStringShape())}
            }))},
            {"structs", new JSONListShape(new JSONObjectShape(
                new Dictionary<string, IJSONShape> {
                    {"name", new JSONStringShape()},
                    {"fields", new JSONListShape(new JSONObjectShape(
                        new Dictionary<string, IJSONShape> {
                            {"name", new JSONStringShape()},
                            {"type_", new JSONStringShape()}
                        }
                    ))}
                }
            ))},
            {"dependencies", new JSONListShape(new JSONObjectShape(new Dictionary<string, IJSONShape> {
                {"path", new JSONStringShape()},
                {"functions", new JSONListShape(new JSONWholeShape())},
                {"structs", new JSONListShape(new JSONStringShape())}
            }))},
            {"clang_config", new JSONListShape(new JSONObjectShape(
                new Dictionary<string, IJSONShape> {
                    {"type", new JSONStringShape()}
                }
            ))},
            {"imports", new JSONListShape(new JSONStringShape())},
            {"ir", new JSONStringShape()},
            {"source", new JSONNullableShape(new JSONStringShape())},
            {"source_type", new JSONStringOptionsShape(new List<string> {"Library", "User"})}
        });
    }
    
    public SPECFileCompiler(string path, string fileText, ShapedJSON obj) {
        this.path = path;
        this.fileText = fileText;
        this.obj = obj;
    }

    public string GetText() {
        return fileText;
    }

    public List<string> ToImports() {
        return obj["imports"].IterList().Select(str=>str.GetString()).ToList();
    }

    Dictionary<string, string> structIds;

    public HashSet<LocatedID> ToStructIDs() {
        HashSet<LocatedID> result = new HashSet<LocatedID>();
        structIds = new Dictionary<string, string>();
        foreach (ShapedJSON struct_ in obj["structs"].IterList()) {
            string name = struct_["name"].GetString();
            result.Add(new LocatedID(path, name));
            structIds[name] = name + " " + path;
        }
        LoadSPECTypes_();
        return result;
    }

    public void LoadSPECTypes_() {
        foreach (ShapedJSON type_Data in obj["types_"].IterList()) {
            List<Type_> generics = new List<Type_>();
            foreach (ShapedJSON generic_Name in type_Data["generics"].IterList()) {
                generics.Add(MakeSPECType_(generic_Name));
            }
            string type_Name = type_Data["name"].GetString();
            if (structIds.ContainsKey(type_Name)) type_Name = structIds[type_Name];
            int? type_Bits = type_Data["bits"].GetIntOrNull();
            Type_ type_ = new Type_(type_Name, type_Bits, generics);
            types_[type_Data["given_name"].GetString()] = type_;
        }
    }

    public void AddStructIDs(HashSet<LocatedID> structIds) {}

    Type_ MakeSPECType_(ShapedJSON str) {
        string text = str.GetString();
        if (!types_.ContainsKey(text)) {
            throw new InvalidJSONException(
                $"Invalid JSON type_ '{text}'", str.GetJSON()
            );
        }
        return types_[text];
    }

    public HashSet<Struct> ToStructs() {
        return obj["structs"].IterList().Select(
            sobj => new Struct(
                path, sobj["name"].GetString(),
                sobj["fields"].IterList().Select(
                    fobj => new Field(
                        fobj["name"].GetString(), 
                        MakeSPECType_(fobj["type_"])
                    )
                ).ToList()
            )
        ).ToHashSet();
    }

    public void SetStructs(HashSet<Struct> structs) {}

    public List<RealFunctionDeclaration> ToDeclarations() {
        return obj["functions"].IterList().Select(func => {
            List<FunctionArgument> arguments = new List<FunctionArgument>();
            List<IPatternSegment> segments = new List<IPatternSegment>();
            List<int> argumentIdxs = new List<int>();
            ShapedJSON template = func["template"];
            for (int i = 0; i < template.ListCount(); i++) {
                ShapedJSON sobj = template[i];
                string type = sobj["type"].GetString();
                Dictionary<string, IJSONShape> fields;
                switch (type) {
                    case "name":
                        fields = new Dictionary<string, IJSONShape> {
                            {"name", new JSONStringShape()}
                        };
                        break;
                    case "text":
                        fields = new Dictionary<string, IJSONShape> {
                            {"text", new JSONStringShape()}
                        };
                        break;
                    case "argument":
                        fields = new Dictionary<string, IJSONShape> {
                            {"name", new JSONStringShape()},
                            {"type_", new JSONStringShape()}
                        };
                        break;
                    default:
                        throw new InvalidJSONException(
                            "Invalid type of template segment", 
                            sobj.GetJSON()
                        );
                }

                sobj = sobj.ToShape(new JSONObjectShape(fields));

                switch (type) {
                    case "name":
                        string name1 = sobj["name"].GetString();
                        segments.Add(new UnitPatternSegment<string>(typeof(Name), name1));
                        break;
                    case "text":
                        string text = sobj["text"].GetString();
                        segments.Add(new TextPatternSegment(text));
                        break;
                    case "argument":
                        string name2 = sobj["name"].GetString();
                        Type_ type_ = MakeSPECType_(sobj["type_"]);
                        FunctionArgument argument = new FunctionArgument(name2, type_);
                        arguments.Add(argument);
                        segments.Add(new TypePatternSegment(typeof(RawSquareGroup)));
                        argumentIdxs.Add(i);
                        break;
                }
            }
            string id = func["id"].GetString();
            string callee = func["callee"].GetString();
            bool takesOwnership = func["takes_ownership"].GetBool();
            bool resultInParams = func["result_in_params"].GetBool();
            Type_ returnType_ = MakeSPECType_(func["return_type_"]);
            Enum.TryParse<FunctionSource>(func["source"].GetString(), out FunctionSource source);
            return (RealFunctionDeclaration)new RealExternalFunction(
                new ConfigurablePatternExtractor<List<IToken>>(
                    segments, new SlotPatternProcessor(argumentIdxs)
                ), arguments, id, path, callee, returnType_, source,
                takesOwnership, resultInParams
            );
        }).ToList();
    }

    public void AddDeclarations(List<RealFunctionDeclaration> declarations) {}

    public Dependencies ToDependencies(Func<string, FileTree> getFile) {
        IEnumerable<(IEnumerable<Struct> structs, IEnumerable<RealFunctionDeclaration> declarations)> pairs = obj["dependencies"].IterList().Select(fobj => {
            FileTree file = getFile(fobj["path"].GetString());
            IFileCompiler oldCompiler;
            HashSet<Struct> structs;
            List<RealFunctionDeclaration> declarations;
            if (file.Compiler.FromCache()) {
                oldCompiler = file.Compiler;
                structs = file.Structs;
                declarations = file.Declarations;
            } else {
                oldCompiler = file.OldCompiler;
                if (oldCompiler == null) throw new RecompilationRequiredException();
                structs = file.OldStructs;
                declarations = file.OldDeclarations;
            }
            IEnumerable<Struct> structDepedencies = fobj["structs"].IterList().Select(sstr => structs.First(
                struct_ => struct_.GetName() == sstr.GetString()
            ));
            if (!file.Compiler.FromCache()) {
                foreach (Struct structDependency in structDepedencies) {
                    bool containsStruct = false;
                    foreach (Struct structNow in file.Structs) {
                        if (structNow.Equals(structDependency)) {
                            containsStruct = true;
                        }
                    }
                    if (!containsStruct) throw new RecompilationRequiredException();
                }
            }
            IEnumerable<RealFunctionDeclaration> functionDependencies = fobj["functions"].IterList().Select(
                dstr => declarations[dstr.GetWhole()]
            );
            if (!file.Compiler.FromCache()) {
                foreach (RealFunctionDeclaration functionDependency in functionDependencies) {
                    if (!file.Declarations.Contains(functionDependency))
                        throw new RecompilationRequiredException();
                }
            }
            return (structDepedencies, functionDependencies);
        });
        return new Dependencies(
            pairs.SelectMany(pair => pair.structs).ToList(), 
            pairs.SelectMany(pair => pair.declarations).ToList()
        );
    }

    public string ToIR(string _) {
        string ir = obj["ir"].GetString();
        return Utils.JoinPaths(Utils.GetDirectoryName(path), ir);
    }

    public string GetSource() {
        return obj["source"].GetStringOrNull();
    }

    public bool FromCache() {
        return GetSource() != null;
    }

    public bool ShouldSaveSPEC() {
        return false;
    }

    public IEnumerable<IClangConfig> GetClangConfig() {
        return obj["clang_config"].IterList().Select(ParseClangConfigFromJSON);
    }

    public static IClangConfig ParseClangConfigFromJSON(ShapedJSON obj) {
        string type = obj["type"].GetString();
        Dictionary<string, IJSONShape> fields;
        
        switch (type) {
            case "constant":
                fields = new Dictionary<string, IJSONShape> {
                    {"config", new JSONStringShape()}
                };
                break;
            default:
                throw new InvalidJSONException(
                    "Invalid type of clang config", 
                    obj.GetJSON()
                );
        }

        obj = obj.ToShape(new JSONObjectShape(fields));

        switch (type) {
            case "constant":
                return new ConstantClangConfig(obj["config"].GetString());
            default:
                throw new InvalidOperationException();
        }
    }

    public FileSourceType GetFileSourceType() {
        Enum.TryParse<FileSourceType>(obj["source_type"].GetString(), 
            out FileSourceType source);
        return source;
    }
}
