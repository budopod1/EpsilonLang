using System;
using System.Linq;
using System.Collections.Generic;

public class ArrayCreation : IParentToken, IValueToken {
    public IParentToken parent { get; set; }
    public CodeSpan span { get; set; }
    
    Type_ type_;
    List<IValueToken> values;
    
    public int Count {
        get {
            return values.Count;
        }
    }
    
    public IToken this[int i] {
        get {
            return values[i];
        }
        set {
            values[i] = ((IValueToken)value);
        }
    }
    
    public ArrayCreation(Type_ type_, List<IValueToken> values) {
        this.type_ = type_;
        this.values = values;
    }
    
    public ArrayCreation(Instantiation instantiation) {
        values = instantiation.GetValues();
        type_ = instantiation.GetType_().GetGeneric(0);
        foreach (IValueToken value in values) {
            if (!value.GetType_().IsConvertibleTo(type_)) {
                throw new SyntaxErrorException(
                    "Elements of array must be convertible to the type of the array", value
                );
            }
        }
    }

    public Type_ GetType_() {
        return new Type_("Array", new List<Type_> {type_});
    }

    public override string ToString() {
        return Utils.WrapName(
            Utils.WrapName(
                GetType().Name,
                type_.ToString(),
                "<", ">"
            ), 
            String.Join(
                ", ", values.ConvertAll<string>(
                    obj => obj.ToString()
                )
            )
        );
    }

    public int Serialize(SerializationContext context) {
        return context.AddInstruction(
            new SerializableInstruction(this, context)
                .AddData("elem_type_", type_.GetJSON())
        );
    }
}
