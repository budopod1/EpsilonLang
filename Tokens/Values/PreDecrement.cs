using System;
using System.Collections.Generic;

public class PreDecrement : UnaryOperation<IAssignableValue>, IValueToken {
    public PreDecrement(IAssignableValue o) : base(o) {}

    public Type_ GetType_() {
        return o.GetType_();
    }

    public override int Serialize(SerializationContext context) {
        IValueToken newValue = new SubOne(o);
        ICompleteLine line = o.AssignTo(newValue);
        line.parent = this;
        newValue.parent = (IParentToken)line;
        context.SerializeInstruction(line);
        return context.SerializeInstruction(newValue);
    }
}
