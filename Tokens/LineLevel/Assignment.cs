using System;
using System.Collections.Generic;

public class Assignment : UnaryOperation<IValueToken>, IVerifier, ICompleteLine, ISerializableToken {
    int id;

    public Assignment(Variable variable, IValueToken o) : base(o) {
        id = variable.GetID();
    }

    public void Verify() {
        Type_ valueType_ = o.GetType_();
        ScopeVar svar = Scope.GetVarByID(this, id);
        Type_ varType_ = svar.GetType_();
        if (!valueType_.IsConvertibleTo(varType_)) {
            throw new SyntaxErrorException(
                $"Cannot assign value of type {valueType_} to variable of type {varType_}", this
            );
        }
    }

    public override int Serialize(SerializationContext context) {
        ScopeVar svar = Scope.GetVarByID(this, id);
        return context.AddInstruction(
            new SerializableInstruction(this, context)
                .AddData("variable", new JSONInt(id))
                .AddData("var_type_", svar.GetType_().GetJSON())
        );
    }
}
