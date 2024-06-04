using System;

public class Variable : IAssignableValue {
    public IParentToken parent { get; set; }
    public CodeSpan span { get; set; }

    string name;
    int id;
    
    public Variable(string name, int id) {
        this.name = name;
        this.id = id;
    }
    
    public Variable(Name source) {
        name = source.GetValue();
        IScope scope = Scope.GetEnclosing(source);
        id = scope.GetIDByName(name).Value;
    }

    public string GetName() {
        return name;
    }

    public int GetID() {
        return id;
    }

    public Type_ GetType_() {
        IScope scope = Scope.GetEnclosing(this);
        ScopeVar svar = scope.GetVarByID(id);
        if (svar == null) {
            throw new SyntaxErrorException(
                $"Cannot find variable with name '{name}' in scope", this
            );
        }
        return svar.GetType_();
    }

    public override string ToString() {
        return Utils.WrapName(GetType().Name, name);
    }

    public int Serialize(SerializationContext context) {
        return context.AddInstruction(
            new SerializableInstruction(this)
                .AddData("variable", new JSONInt(GetID()))
        );
    }
    
    public ICompleteLine AssignTo(IValueToken value) {
        return new Assignment(this, value);
    }
}