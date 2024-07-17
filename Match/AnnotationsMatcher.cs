using System;
using System.Collections.Generic;

public class AnnotationsMatcher : IMatcher {
    public Match Match(IParentToken tokens) {
        for (int i = 0; i < tokens.Count; i++) {
            TextToken stoken = tokens[i] as TextToken;
            if (stoken == null || stoken.GetText() != "@") continue;
            List<IToken> matched = new List<IToken> {stoken};
            string type = null;
            if (i + 1 < tokens.Count) {
                Name name = tokens[i+1] as Name;
                matched.Add(name);
                if (name != null) {
                    type = name.GetValue();
                }
            }
            if (type == null) {
                throw new SyntaxErrorException(
                    "Expected annotation name after toplevel '@'", stoken
                );
            }
            List<IToken> arguments = new List<IToken>();
            bool completed = false;
            int j = i + 2;
            for (; j < tokens.Count; j++) {
                IToken token = tokens[j];
                if (token is TextToken) {
                    string text = ((TextToken)token).GetText();
                    if (text == "@") {
                        completed = true;
                        j--;
                        break;
                    } else if (text == ";") {
                        completed = true;
                        matched.Add(token);
                        break;
                    }
                }
                arguments.Add(token);
                matched.Add(token);
            }
            if (!completed) {
                throw new SyntaxErrorException(
                    "Expected another annotation argument, '@', or ';', found EOF",
                    matched[matched.Count - 1]
                );
            }
            RawAnnotation annotation = new RawAnnotation(type, arguments);
            return new Match(i, j, new List<IToken> {annotation}, matched);
        }
        return null;
    }
}
