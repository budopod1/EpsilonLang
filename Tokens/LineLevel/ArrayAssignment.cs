using System;

public class ArrayAssignment : TrinaryOperation<IValueToken, IValueToken, IValueToken>, IVerifier, ICompleteLine {
    public ArrayAssignment(IValueToken array, IValueToken index, IValueToken value) : base(array, index, value) {}
    public ArrayAssignment(ArrayAccess access, IValueToken value) : base(access.GetArray(), access.GetIndex(), value) {}

    public void Verify() {
        if (!o2.GetType_().IsConvertibleTo(new Type_("Z")))
            throw new SyntaxErrorException(
                $"Arrays can only be indexed with integers, not {o2.GetType_()}", this
            );
        Type_ valType_ = o1.GetType_().GetGeneric(0);
        if (!o3.GetType_().IsConvertibleTo(valType_))
            throw new SyntaxErrorException(
                $"Cannot assign value of type {o3.GetType_()} to an index in an array of type {o1.GetType_()}", this
            );
    }

    public override int Serialize(SerializationContext context) {
        return context.AddInstruction(
            new SerializableInstruction(this, context)
                .AddData("elem_type_", o1.GetType_().GetGeneric(0).GetJSON())
        );
    }
}
