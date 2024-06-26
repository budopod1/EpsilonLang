using System;
using System.Linq;
using System.Collections.Generic;

public class Program : TreeToken, IVerifier {
    string path;
    HashSet<LocatedID> structIds = new HashSet<LocatedID>();
    int functionIDCounter = 0;
    int scopeVarIDCounter = 0;
    HashSet<Struct> structsHere = new HashSet<Struct>();
    HashSet<Struct> structs = new HashSet<Struct>();
    List<RealFunctionDeclaration> externalDeclarations = new List<RealFunctionDeclaration>();

    public Program(string path, List<IToken> tokens) : base(tokens) {
        this.path = path;
    }

    public Program(string path, List<IToken> tokens, HashSet<LocatedID> structIds, int functionIDCounter, int scopeVarIDCounter, HashSet<Struct> structs, HashSet<Struct> structsHere, List<RealFunctionDeclaration> externalDeclarations) : base(tokens) {
        this.path = path;
        this.structIds = structIds;
        this.functionIDCounter = functionIDCounter;
        this.scopeVarIDCounter = scopeVarIDCounter;
        this.structs = structs;
        this.structsHere = structsHere;
        this.externalDeclarations = externalDeclarations;
    }

    public HashSet<LocatedID> GetStructIDs() {
        return structIds;
    }

    public void UpdateParents() {
        TokenUtils.UpdateParents(this);
        parent = null;
    }

    public void AddStructIDs(HashSet<LocatedID> structIds) {
        this.structIds.UnionWith(structIds);
    }

    public int GetFunctionID() {
        return functionIDCounter++;
    }

    public int GetScopeVarID() {
        return scopeVarIDCounter++;
    }

    public void SetStructsHere(HashSet<Struct> structs) {
        structsHere = structs;
    }

    public void SetStructs(HashSet<Struct> structs) {
        this.structs = structs;
    }

    public HashSet<Struct> GetStructs() {
        return structs;
    }

    public HashSet<Struct> GetStructsHere() {
        return structsHere;
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
        return new Program(path, tokens, structIds, functionIDCounter, scopeVarIDCounter, structs, structsHere, externalDeclarations);
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
            if (struct_.GetID() == name) return struct_;
        }
        Console.WriteLine(path); // temp
        throw new ArgumentException($"No struct found for type_ {type_.ToString()}");
    }

    public IJSONValue GetJSON() {
        JSONObject obj = new JSONObject();
        JSONList functions = new JSONList();
        foreach (IToken token in this) {
            if (token is Function) {
                functions.Add(((Function)token).GetFullJSON());
            }
        }
        obj["functions"] = functions;
        obj["module_functions"] = new JSONList(
            externalDeclarations.Select(declaration => declaration.GetJSON())
        );
        obj["structs"] = new JSONList(structs.Select(struct_ => struct_.GetJSON()));
        obj["path"] = new JSONString(path);
        return obj;
    }
}
