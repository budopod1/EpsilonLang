using System;

public class And : BinaryOperation<IValueToken, IValueToken>, IValueToken {
    public And(IValueToken o1, IValueToken o2) : base(o1, o2) {}

    public Type_ GetType_() {
        return new Type_("Bool");
    }

    public int Serialize(SerializationContext context) {
        return context.AddInstruction(
            new SerializableInstruction(this, context)
        );
    }
}
