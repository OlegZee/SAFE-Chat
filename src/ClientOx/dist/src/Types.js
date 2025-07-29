import { Record, Union } from "../fable_modules/fable-library-js.4.25.0/Types.js";
import { record_type, union_type, obj_type } from "../fable_modules/fable-library-js.4.25.0/Reflection.js";
import { Route_$reflection } from "./Router.js";
import { ChatState_$reflection } from "./Chat/Types.js";

export class Msg extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["ChatDataMsg"];
    }
}

export function Msg_$reflection() {
    return union_type("App.Types.Msg", [], Msg, () => [[["Item", obj_type]]]);
}

export class Model extends Record {
    constructor(currentPage, chat) {
        super();
        this.currentPage = currentPage;
        this.chat = chat;
    }
}

export function Model_$reflection() {
    return record_type("App.Types.Model", [], Model, () => [["currentPage", Route_$reflection()], ["chat", ChatState_$reflection()]]);
}

