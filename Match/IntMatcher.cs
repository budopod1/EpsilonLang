using System;
using System.Linq;
using System.Collections.Generic;

public class IntMatcher : IMatcher {
    Program program;

    public IntMatcher(Program program) {
        this.program = program;
    }
    
    public Match Match(IParentToken tokens) {
        for (int i = 0; i < tokens.Count; i++) {
            List<IToken> replaced = new List<IToken>();
            bool anyMatch = false;
            bool content = false;
            int j;
            for (j = i; j < tokens.Count; j++) {
                IToken token = tokens[j];
                if (!(token is TextToken)) {
                    break;
                }
                bool foundMatch = false;
                string digit = ((TextToken)token).GetText();
                if (digit == "-" && !anyMatch) {
                    foundMatch = true;
                } else if ("1234567890".Contains(digit)) {
                    foundMatch = true;
                    content = true;
                }
                anyMatch |= foundMatch;
                if (!foundMatch) {
                    break;
                }
                replaced.Add(token);
            }
            if (anyMatch && content) {
                string matchedString = String.Join(
                    "", replaced.Select(
                        (IToken sub) => ((TextToken)sub).GetText()
                    )
                );
                int constant = program.GetConstants().AddConstant(
                    IntConstant.FromString(matchedString)
                );
                List<IToken> replacement = new List<IToken> {
                    new ConstantValue(constant)
                };
                return new Match(i, j-1, replacement, replaced);
            }
        }
        return null;
    }
}
