import * as react from "react";
import { HTMLAttr } from "../../fable_modules/Fable.React.10.0.0-alpha.1/Fable.React.Props.fs.js";
import { printf, toText } from "../../fable_modules/fable-library-js.4.25.0/String.js";
import { keyValueList } from "../../fable_modules/fable-library-js.4.25.0/MapUtil.js";

export function root(_arg) {
    let matchResult, url;
    if (_arg != null) {
        if (_arg === "") {
            matchResult = 0;
        }
        else {
            matchResult = 1;
            url = _arg;
        }
    }
    else {
        matchResult = 0;
    }
    switch (matchResult) {
        case 0:
            return react.createElement("div", {
                className: "fs-avatar",
            });
        default: {
            const props_2 = [new HTMLAttr(64, ["fs-avatar"]), ["style", {
                backgroundImage: toText(printf("url(%s)"))(url),
            }]];
            return react.createElement("div", keyValueList(props_2, 1));
        }
    }
}

