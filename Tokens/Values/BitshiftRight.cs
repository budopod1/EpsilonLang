using System;

public class BitshiftRight : BinaryOperation<IValueToken, IValueToken>, IValueToken {
    public BitshiftRight(IValueToken o1, IValueToken o2) : base(o1, o2) {}

    public Type_ GetType_() {
        return o1.GetType_();
    }
}
