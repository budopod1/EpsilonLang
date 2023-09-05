using System;
using System.Collections.Generic;

public class RawFuncSignatureMatcher : IMatcher {
    public Match Match(IParentToken tokens) {
        bool wasNL = true;
        for (int i = 0; i < tokens.Count; i++) {
            IToken stoken = tokens[i];
            if (wasNL) {
                List<IToken> matched = new List<IToken>();
                List<IToken> before = new List<IToken>();
                List<IToken> after = new List<IToken>();
                bool hasHashtag = false;
                for (int j = i; j < tokens.Count; j++) {
                    IToken token = tokens[j];
                    TextToken ttoken2 = token as TextToken;
                    if (ttoken2 != null) {
                        string txt = ttoken2.GetText();
                        if (txt == "#") {
                            hasHashtag = true;
                            matched.Add(token);
                            continue;
                        } else if (txt == "\n" || txt == "{") {
                            if (hasHashtag) {
                                return new Match(
                                    i, j-1, new List<IToken> {
                                        new RawFuncSignature(
                                            new RawFuncReturnType_(before),
                                            new RawFuncTemplate(after)
                                        )
                                    }, matched
                                );
                            } else {
                                break;
                            }
                        }
                    }
                    if (hasHashtag) {
                        after.Add(token);
                    } else {
                        before.Add(token);
                    }
                    matched.Add(token);
                }
            }
            TextToken ttoken = stoken as TextToken;
            if (ttoken != null && ttoken.GetText() == "\n") {
                wasNL = true;
            } else {
                wasNL = false;
            }
        }
        return null;
    }
}
