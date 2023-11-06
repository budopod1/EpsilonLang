using System;

public class Return : UnaryOperation<IValueToken>, IVerifier, ICompleteLine {
    public Return(IValueToken o) : base(o) {}

    public void Verify() {
        Function func = TokenUtils.GetParentOfType<Function>(this);
        Type_ returnType_ = func.GetReturnType_();
        if (!o.GetType_().IsConvertibleTo(returnType_)) {
            throw new SyntaxErrorException(
                $"Cannot return {o.GetType_()}; function expects {returnType_} return type", this
            );
        }
    }

    public int Serialize(SerializationContext context) {
        return context.AddInstruction(new SerializableInstruction(
            this, context
        ));
    }
}
