using System;

public class MemberAssignment : BinaryOperation<IValueToken, IValueToken>, IVerifier, ICompleteLine {
    string member;
    
    public MemberAssignment(IValueToken o1, string member, IValueToken o2) : base(o1, o2) {
        this.member = member;
    }

    public MemberAssignment(MemberAccess access, IValueToken o2) : base(access.Sub(), o2) {
        member = access.GetMember();
    }

    public void Verify() {
        Program program = TokenUtils.GetParentOfType<Program>(this);
        Type_ type_ = o1.GetType_();
        Struct struct_ = program.GetStructFromType_(type_);
        if (struct_ == null)
            throw new SyntaxErrorException(
                $"You can assign to members of struct types, not {type_}", this
            );
        Field field = struct_.GetField(member);
        if (field == null)
            throw new SyntaxErrorException(
                $"Struct {struct_.GetName()} has no member {member}", this
            );
        if (!o2.GetType_().IsConvertibleTo(field.GetType_())) {
            throw new SyntaxErrorException(
                $"Cannot assign value of type {o2.GetType_()} to member {member} of type {field.GetType_()}", this
            );
        }
    }

    public override string ToString() {
        return Utils.WrapName(
            GetType().Name, 
            $"{o1.ToString()}, {member}, {o2.ToString()}"
        );
    }

    public override int Serialize(SerializationContext context) {
        return context.AddInstruction(
            new SerializableInstruction(this, context)
                .AddData("member", new JSONString(member))
                .AddData("struct_type_", o1.GetType_().GetJSON())
        );
    }
}
