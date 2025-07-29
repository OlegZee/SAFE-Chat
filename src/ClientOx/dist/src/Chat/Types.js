import { Union, Record } from "../../fable_modules/fable-library-js.4.25.0/Types.js";
import { union_type, record_type, option_type, class_type, string_type, obj_type } from "../../fable_modules/fable-library-js.4.25.0/Reflection.js";
import { Msg_$reflection, UserInfo_$reflection, ChannelData_$reflection, ChannelInfo_$reflection } from "../Channel/Types.js";
import { empty } from "../../fable_modules/fable-library-js.4.25.0/Map.js";
import { comparePrimitives } from "../../fable_modules/fable-library-js.4.25.0/Util.js";

export class ChatData extends Record {
    constructor(socket, ChannelList, Channels, NewChanName) {
        super();
        this.socket = socket;
        this.ChannelList = ChannelList;
        this.Channels = Channels;
        this.NewChanName = NewChanName;
    }
}

export function ChatData_$reflection() {
    return record_type("Chat.Types.ChatData", [], ChatData, () => [["socket", obj_type], ["ChannelList", class_type("Microsoft.FSharp.Collections.FSharpMap`2", [string_type, ChannelInfo_$reflection()])], ["Channels", class_type("Microsoft.FSharp.Collections.FSharpMap`2", [string_type, ChannelData_$reflection()])], ["NewChanName", option_type(string_type)]]);
}

export function ChatData_get_Empty() {
    return new ChatData((() => {
        throw 1;
    })(), empty({
        Compare: comparePrimitives,
    }), empty({
        Compare: comparePrimitives,
    }), undefined);
}

export class ChatState extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["NotConnected", "Connected"];
    }
}

export function ChatState_$reflection() {
    return union_type("Chat.Types.ChatState", [], ChatState, () => [[], [["Item1", UserInfo_$reflection()], ["Item2", ChatData_$reflection()]]]);
}

export class AppMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Nop", "ChannelMsg", "SetNewChanName", "CreateJoin", "Join", "Leave"];
    }
}

export function AppMsg_$reflection() {
    return union_type("Chat.Types.AppMsg", [], AppMsg, () => [[], [["Item1", string_type], ["Item2", Msg_$reflection()]], [["Item", option_type(string_type)]], [], [["chanId", string_type]], [["chanId", string_type]]]);
}

