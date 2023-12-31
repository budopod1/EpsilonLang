using System;
using System.Collections.Generic;

public class TextsPatternSegment : IPatternSegment {
    List<string> texts;

    public List<string> GetTexts() {
        return texts;
    }
    
    public TextsPatternSegment(List<string> texts) {
        this.texts = texts;
    }

    public bool Matches(IToken token) {
        return (token is TextToken 
            && texts.Contains(((TextToken)token).GetText()));
    }

    public bool Equals(IPatternSegment obj) {
        TextsPatternSegment other = obj as TextsPatternSegment;
        if (other == null) return false;
        return Utils.ListEqual<string>(texts, other.GetTexts());
    }
}
