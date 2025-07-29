import { Union } from "../fable_modules/fable-library-js.4.25.0/Types.js";
import { union_type, string_type } from "../fable_modules/fable-library-js.4.25.0/Reflection.js";

export class Route extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Overview", "Channel"];
    }
}

export function Route_$reflection() {
    return union_type("Router.Route", [], Route, () => [[], [["Item", string_type]]]);
}

export const route = (() => {
    throw 1;
})();

export function toHash(_arg) {
    if (_arg.tag === 1) {
        return "#channel/" + _arg.fields[0];
    }
    else {
        return "#overview";
    }
}

