using System;
using System.Linq;
using System.Collections.Generic;

public class Program : TreeToken, IVerifier {
    string path;
    HashSet<string> baseType_Names = null;
    int functionIDCounter = 0;
    int scopeVarIDCounter = 0;
    List<Struct> structs = new List<Struct>();
    List<RealFunctionDeclaration> externalDeclarations = new List<RealFunctionDeclaration>();

    public Program(string path, List<IToken> tokens) : base(tokens) {
        this.path = path;
    }

    public Program(string path, List<IToken> tokens, HashSet<string> baseType_Names, int functionIDCounter, int scopeVarIDCounter, List<Struct> structs, List<RealFunctionDeclaration> externalDeclarations) : base(tokens) {
        this.path = path;
        this.baseType_Names = baseType_Names;
        this.functionIDCounter = functionIDCounter;
        this.scopeVarIDCounter = scopeVarIDCounter;
        this.structs = structs;
        this.externalDeclarations = externalDeclarations;
    }

    public HashSet<string> GetBaseType_Names() {
        return baseType_Names;
    }

    public void UpdateParents() {
        TokenUtils.UpdateParents(this);
        parent = null;
    }

    public void SetBaseType_Names(HashSet<string> baseType_Names) {
        this.baseType_Names = baseType_Names;
    }

    public void AddBaseTypes_(HashSet<string> baseType_Names) {
        this.baseType_Names.UnionWith(baseType_Names);
    }

    public int GetFunctionID() {
        return functionIDCounter++;
    }

    public int GetScopeVarID() {
        return scopeVarIDCounter++;
    }

    public void SetStructs(List<Struct> structs) {
        this.structs = structs;
    }

    public void AddStructs(List<Struct> structs) {
        this.structs.AddRange(structs);
    }

    public List<Struct> GetStructs() {
        return structs;
    }

    public void AddExternalDeclarations(List<RealFunctionDeclaration> declarations) {
        externalDeclarations.AddRange(declarations);
    }

    public List<RealFunctionDeclaration> GetExternalDeclarations() {
        return externalDeclarations;
    }

    public string GetPath() {
        return path;
    }

    protected override TreeToken _Copy(List<IToken> tokens) {
        return new Program(path, tokens, baseType_Names, functionIDCounter, scopeVarIDCounter, structs, externalDeclarations);
    }

    public void Verify() {
        bool foundMain = false;
        foreach (IToken token in this) {
            if (!(token is ITopLevel)) {
                throw new SyntaxErrorException(
                    "Invalid toplevel syntax", token
                );
            }
            Function func = token as Function;
            if (func != null) {
                if (func.IsMain()) {
                    if (foundMain) {
                        throw new SyntaxErrorException(
                            "Only one main function can be defined", func
                        );
                    }
                    foundMain = true;
                }
            }
        }
    }

    public Struct GetStructFromType_(Type_ type_) {
        string name = type_.GetBaseType_().GetName();
        foreach (Struct struct_ in structs) {
            if (struct_.GetName() == name) return struct_;
        }
        return null;
    }

    public IJSONValue GetJSON() {
        JSONObject obj = new JSONObject();
        JSONList functions = new JSONList();
        foreach (IToken token in this) {
            if (token is Function) {
                functions.Add(((Function)token).GetJSON());
            }
        }
        obj["functions"] = functions;
        obj["module_functions"] = new JSONList(
            externalDeclarations.Select(declaration => {
                JSONObject dobj = new JSONObject();
                dobj["id"] = new JSONString(declaration.GetID());
                dobj["arguments"] = new JSONList(declaration.GetArguments().Select(
                    argument => argument.GetJSON()
                ));
                dobj["return_type_"] = declaration.GetReturnType_().GetJSON();
                return dobj;
            })
        );
        obj["structs"] = new JSONList(structs.Select(struct_ => struct_.GetJSON()));
        obj["path"] = new JSONString(path);
        return obj;
    }
}
