using System;
using System.Collections.Generic;

public class OR : IParentToken, IValueToken {
    public IParentToken parent { get; set; }
    
    IValueToken o1;
    IValueToken o2;
    
    public int Count {
        get { return 2; }
    }
    
    public IToken this[int i] {
        get {
            if (i == 0) return o1;
            return o2;
        }
        set {
            if (i == 0) {
                o1 = (IValueToken)value;
            } else {
                o2 = (IValueToken)value;
            }
        }
    }
    
    public OR(IValueToken o1, IValueToken o2) {
        this.o1 = o1;
        this.o2 = o2;
    }

    public override string ToString() {
        return Utils.WrapName(
            this.GetType().Name, $"{o1.ToString()}, {o2.ToString()}"
        );
    }

    public Type_ GetType_() {
        return Type_.Common(o1.GetType_(), o2.GetType_());
    }
}