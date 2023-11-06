using System;
using System.Collections.Generic;

public class MemberAccess : UnaryOperation<IValueToken>, IValueToken, IVerifier {
    string member;
    
    public MemberAccess(IValueToken o, Name member) : base(o) {
        this.member = member.GetValue();
    }

    public Type_ GetType_() {
        Program program = TokenUtils.GetParentOfType<Program>(this);
        Type_ type_ = o.GetType_();
        Struct struct_ = program.GetStructFromType_(type_);
        if (struct_ == null)
            throw new SyntaxErrorException(
                $"You can access members of struct types, not {type_}", this
            );
        Field field = struct_.GetField(member);
        if (field == null)
            throw new SyntaxErrorException(
                $"Struct {struct_.GetName()} has no member {member}", this
            );
        return field.GetType_();
    }

    public string GetMember() {
        return member;
    }

    public override string ToString() {
        return Utils.WrapName(
            GetType().Name,  o.ToString() + ", " + member
        );
    }

    public void Verify() {
        GetType_();
    }

    public int Serialize(SerializationContext context) {
        return context.AddInstruction(
            new SerializableInstruction(
                this, context
            ).AddData("member", new JSONString(member))
        );
    }
}
