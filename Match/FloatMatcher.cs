using System;
using System.Collections.Generic;

public class FloatMatcher : IMatcher {
    public Match Match(ParentToken tokens) {
        for (int i = 0; i < tokens.Count; i++) {
            List<Token> replaced = new List<Token>();
            bool dot = false;
            bool anyMatch = false;
            int j;
            for (j = i; j < tokens.Count; j++) {
                Token token = tokens[j];
                if (!(token is TextToken)) {
                    break;
                }
                string digit = ((TextToken)token).GetText();
                bool foundMatch = false;
                if (digit == "-" && !anyMatch) {
                    foundMatch = true;
                } else if ("1234567890".Contains(digit)) {
                    foundMatch = true;
                } else if (digit == "." && !dot) {
                    dot = true;
                    foundMatch = true;
                }
                anyMatch |= foundMatch;
                if (!foundMatch) {
                    break;
                }
                replaced.Add(token);
            }
            if (anyMatch && dot) {
                return new Match(i, j-1, replaced, new List<Token>());
            }
        }
        return null;
    }
}