using System;

public class Variable : IValueToken {
    public IParentToken parent { get; set; }

    string name;
    int id;
    
    public Variable(string name, int id) {
        this.name = name;
        this.id = id;
    }
    
    public Variable(Name source) {
        name = source.GetValue();
        Scope scope = Scope.GetEnclosing(source);
        int newID = scope.GetIDByName(name).Value;
    }

    public string GetName() {
        return name;
    }

    public int GetID() {
        return id;
    }

    public Type_ GetType_() {
        Scope scope = Scope.GetEnclosing(this);
        ScopeVar svar = scope.GetVarByID(id);
        if (svar == null) return Type_.Unknown();
        return svar.GetType_();
    }

    public override string ToString() {
        return Utils.WrapName(this.GetType().Name, this.name);
    }
}
