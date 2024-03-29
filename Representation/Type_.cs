using System;
using System.Linq;
using System.Collections.Generic;

public class Type_ : IEquatable<Type_> {
    // https://en.wikipedia.org/wiki/
    // Set_(mathematics)#Special_sets_of_numbers_in_mathematics

    public static Type_ Unknown() {
        return new Type_("Unknown");
    }

    public static Type_ Void() {
        return new Type_("Void");
    }

    public static Type_ String() {
        return new Type_("Array", new List<Type_> {new Type_("Byte")});
    }
    
    BaseType_ baseType_;
    List<Type_> generics;
    public static List<Type_> FinalTypes_ = new List<Type_>();

    public static Type_ Any() {
        return new Type_("Any");
    }

    public static bool AreCompatible(Type_ a, Type_ b) {
        if (a.Equals(b)) return true;
        BaseType_ abt = a.GetBaseType_();
        BaseType_ bbt = b.GetBaseType_();
        if (abt.IsAny() && !bbt.IsNon()) 
            return true;
        if (bbt.IsAny() && !abt.IsNon()) 
            return true;
        if (a.HasGenerics() || b.HasGenerics()) 
            return false;
        bool aToB = abt.IsConvertibleTo(bbt);
        bool bToA = bbt.IsConvertibleTo(abt);
        return aToB || bToA;
    }

    public static Type_ Common(Type_ a, Type_ b) {
        if (a.Equals(b)) return a;
        if (a.HasGenerics() || b.HasGenerics()) 
            return Unknown();
        BaseType_ abt = a.GetBaseType_();
        BaseType_ bbt = b.GetBaseType_();
        bool aToB = abt.IsConvertibleTo(bbt);
        bool bToA = bbt.IsConvertibleTo(abt);
        if (aToB && bToA) {
            if (bbt.GetBits() > abt.GetBits()) {
                return b;
            } else {
                return a;
            }
        } else if (aToB) {
            return b;
        } else if (bToA) {
            return a;
        } else {
            return Unknown();
        }
    }

    public static Type_ CommonSpecific(Type_ a, Type_ b, string name) {
        if (a.Equals(b) && a.GetBaseType_().GetName()==name) return a;
        if (a.HasGenerics() || b.HasGenerics()) 
            return Unknown();
        int? abits = a.GetBaseType_().GetBits();
        int? bbits = b.GetBaseType_().GetBits();
        int? bits = null;
        if (abits==null) {
            bits = bbits;
        } else if (bbits==null) {
            bits = abits;
        } else {
            bits = Math.Max(abits.Value, bbits.Value);
        }
        return new Type_(name, bits);
    }

    public Type_(BaseType_ baseType_, List<Type_> generics = null) {
        this.baseType_ = baseType_;
        if (generics == null) {
            generics = new List<Type_>();
        }
        CheckGenerics(baseType_, generics);
        this.generics = generics;
    }

    public Type_(string name, int? bits, List<Type_> generics = null) {
        baseType_ = new BaseType_(name, bits);
        if (generics == null) {
            generics = new List<Type_>();
        }
        CheckGenerics(baseType_, generics);
        this.generics = generics;
    }

    public Type_(string name, List<Type_> generics = null) {
        baseType_ = new BaseType_(name);
        if (generics == null) {
            generics = new List<Type_>();
        }
        CheckGenerics(baseType_, generics);
        this.generics = generics;
    }

    void CheckGenerics(BaseType_ baseType_, List<Type_> generics) {
        if (generics.Count != baseType_.GenericsAmount()) {
            throw new IllegalType_GenericsException(
                $"Incorrect number of generics on base type {baseType_} (got {generics.Count}, expected {baseType_.GenericsAmount()})"
            );
        }
    }

    public static Type_ FromUserBaseType_(UserBaseType_ baseType_, List<Type_> generics = null) {
        if (generics == null) generics = new List<Type_>();
        Type_ result = baseType_.ToType_(generics);
        if (result.GetBaseType_().GetName() == "Optional") {
            if (!generics[0].GetBaseType_().IsOptionable()) {
                throw new IllegalType_GenericsException(
                    $"Invalid generic {generics[0]} for Optional type"
                );
            }
        }
        return result;
    }

    public Type_ WithGenerics(List<Type_> generics) {
        return new Type_(baseType_, generics);
    }

    public BaseType_ GetBaseType_() {
        return baseType_;
    }

    public List<Type_> GetGenerics() {
        return generics;
    }

    public Type_ GetGeneric(int i) {
        return generics[i];
    }

    public bool HasGenerics() {
        return generics.Count > 0;
    }

    public int GenericCount() {
        return generics.Count;
    }

    bool IsConvertibleOptionalTo(Type_ other) {
        if (other.GetBaseType_().GetName() != "Optional") return false;
        return Equals(other.GetGeneric(0));
    }

    bool IsConvertibleNullTo(Type_ other) {
        if (baseType_.GetName() != "Null") return false;
        return other.GetBaseType_().GetName() == "Optional";
    }

    public bool IsConvertibleTo(Type_ other) {
        BaseType_ otherBaseType_ = other.GetBaseType_();
        if (baseType_.IsAny() && !otherBaseType_.IsNon()) 
            return true;
        if (otherBaseType_.IsAny() && !baseType_.IsNon()) 
            return true;
        if (IsConvertibleOptionalTo(other)) return true;
        if (IsConvertibleNullTo(other)) return true;
        if (HasGenerics())
            return Equals(other);
        if (other.HasGenerics()) return false;
        if (baseType_.IsConvertibleTo(otherBaseType_))
            return true;
        return false;
    }

    public bool IsEquivalentTo(Type_ other) {
        BaseType_ otherBaseType_ = other.GetBaseType_();
        if (IsConvertibleOptionalTo(other)) return true;
        if (IsConvertibleNullTo(other)) return true;
        if (HasGenerics()) {
            if (!baseType_.Equals(otherBaseType_)) return false;
            List<Type_> otherGenerics = other.GetGenerics();
            if (generics.Count != otherGenerics.Count) return false;
            for (int i = 0; i < generics.Count; i++) {
                if (!generics[i].IsEquivalentTo(otherGenerics[i])) 
                    return false;
            }
            return true;
        } else {
            return baseType_.IsEquivalentTo(otherBaseType_);
        }
    }

    public bool IsCastableTo(Type_ other) {
        BaseType_ otherBaseType_ = other.GetBaseType_();
        if (baseType_.IsAny() && !otherBaseType_.IsNon()) 
            return true;
        if (otherBaseType_.IsAny() && !baseType_.IsNon()) 
            return true;
        if (IsEquivalentTo(other)) return true;
        if (HasGenerics()) return Equals(other);
        if (other.HasGenerics()) return false;
        if (baseType_.IsCastableTo(otherBaseType_))
            return true;
        return false;
    }

    public bool Equals(Type_ other) {
        BaseType_ otherBaseType_ = other.GetBaseType_();
        if (baseType_.IsAny() && !otherBaseType_.IsNon()) 
            return true;
        if (otherBaseType_.IsAny() && !baseType_.IsNon()) 
            return true;
        if (baseType_.Equals(otherBaseType_)) {
            return GenericsEqual(other);
        }
        return false;
    }

    public bool GenericsEqual(Type_ other) {
        return Utils.ListEqual<Type_>(generics, other.GetGenerics());
    }

    public override string ToString() {
        string genericStr = "";
        bool first = true;
        foreach (Type_ generic in generics){
            if (!first) {
                genericStr += ", ";
            }
            genericStr += generic.ToString();
            first = false;
        }
        return Utils.WrapName(
            baseType_.ToString(), genericStr, "<", ">"
        );
    }

    public IJSONValue GetJSON(bool isFinalType_=true) {
        if (isFinalType_) FinalTypes_.Add(this);
        JSONObject obj = new JSONObject();
        obj["name"] = new JSONString(baseType_.GetName());
        obj["bits"] = new JSONInt(baseType_.GetBitsOrDefaultIfMeaningful());
        obj["generics"] = new JSONList(generics.Select(generic=>generic.GetJSON()));
        return obj;
    }
}
