using System;
using System.Collections.Generic;

public class StringConstant : IConstant {
    string value;

    public static Dictionary<char, string> BackslashReplacements = new Dictionary<char, string> {
        {'n', "\n"},
        {'t', "\t"},
        {'r', "\r"},
        {'\\', "\\"},
    };

    public StringConstant(string value) {
        this.value = value;
    }

    public static StringConstant FromString(string value) {
        return new StringConstant(JSONTools.FromLiteral(value));
    }

    public Type_ GetType_() {
        return Type_.String();
    }

    public string GetValue() {
        return value;
    }

    public bool IsTruthy() {
        return value.Length > 0;
    }

    public IJSONValue GetJSON() {
        throw new InvalidOperationException("String constants cannot appear in the final IR");
    }
}
