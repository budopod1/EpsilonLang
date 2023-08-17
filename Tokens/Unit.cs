using System;

public class Unit<T> : Token {
    T value;
    
    public Unit(T value) {
        this.value = value;
    }

    public T GetValue() {
        return value;
    }

    public override string ToString() {
        return Utils.WrapName(this.GetType().Name, this.value.ToString());
    }
}