using System;
using System.Linq;
using System.Collections.Generic;

public class Program : TreeToken, IVerifier {
    string path;
    List<string> baseType_Names = null;
    
    public Program(string path, List<IToken> tokens) : base(tokens) {
        this.path = path;
    }
    
    public Program(string path, List<IToken> tokens, List<string> baseType_Names) : base(tokens) {
        this.path = path;
        this.baseType_Names = baseType_Names;
    }

    public List<string> GetBaseType_Names() {
        return baseType_Names;
    }

    public void UpdateParents() {
        TokenUtils.UpdateParents(this);
        parent = null;
    }

    public void SetBaseType_Names(List<string> baseType_Names) {
        this.baseType_Names = baseType_Names;
    }
    
    protected override TreeToken _Copy(List<IToken> tokens) {
        return new Program(path, tokens, baseType_Names);
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
        foreach (IToken token in this) {
            Struct struct_ = token as Struct;
            if (struct_ == null) continue;
            if (struct_.GetName() == name) {
                return struct_;
            } 
        }
        return null;
    }

    public IJSONValue GetJSON() {
        JSONObject obj = new JSONObject();
        JSONList functions = new JSONList();
        JSONList structs = new JSONList();
        foreach (IToken token in this) {
            if (token is Function) {
                functions.Add(((Function)token).GetJSON());
            } else if (token is Struct) {
                structs.Add(((Struct)token).GetJSON());
            }
        }
        List<Type_> uniqueArrayTypes_ = new List<Type_>();
        List<Type_> arrayTypes_ = Type_.FinalTypes_.Where(
            type_=>type_.GetBaseType_().GetName()=="Array"
        ).ToList();
        foreach (Type_ type_ in arrayTypes_) {
            bool unique = true;
            foreach (Type_ otype_ in uniqueArrayTypes_) {
                if (type_.Equals(otype_)) {
                    unique = false;
                    break;
                }
            }
            if (unique) uniqueArrayTypes_.Add(type_);
        }
        Type_.FinalTypes_ = new List<Type_>();
        obj["functions"] = functions;
        obj["structs"] = structs;
        obj["arrays"] = new JSONList(uniqueArrayTypes_.Select(
            type_=>type_.GetJSON(false)
        ));
        obj["path"] = new JSONString(path);
        return obj;
    }
}
