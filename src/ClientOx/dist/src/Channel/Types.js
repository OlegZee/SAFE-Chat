import { Union, Record } from "../../fable_modules/fable-library-js.4.25.0/Types.js";
import { tuple_type, list_type, class_type, int32_type, union_type, record_type, option_type, bool_type, string_type } from "../../fable_modules/fable-library-js.4.25.0/Reflection.js";
import { defaultOf } from "../../fable_modules/fable-library-js.4.25.0/Util.js";

export class UserInfo extends Record {
    constructor(Id, Nick, Status, IsBot, Online, ImageUrl, isMe) {
        super();
        this.Id = Id;
        this.Nick = Nick;
        this.Status = Status;
        this.IsBot = IsBot;
        this.Online = Online;
        this.ImageUrl = ImageUrl;
        this.isMe = isMe;
    }
}

export function UserInfo_$reflection() {
    return record_type("Channel.Types.UserInfo", [], UserInfo, () => [["Id", string_type], ["Nick", string_type], ["Status", string_type], ["IsBot", bool_type], ["Online", bool_type], ["ImageUrl", option_type(string_type)], ["isMe", bool_type]]);
}

export function UserInfo_get_Anon() {
    return new UserInfo("0", "anonymous", "", false, true, undefined, false);
}

export class Message extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UserMessage", "SystemMessage"];
    }
}

export function Message_$reflection() {
    return union_type("Channel.Types.Message", [], Message, () => [[["text", string_type], ["author", UserInfo_$reflection()]], [["text", string_type]]]);
}

export class Envelope$1 extends Record {
    constructor(Id, Ts, Content) {
        super();
        this.Id = (Id | 0);
        this.Ts = Ts;
        this.Content = Content;
    }
}

export function Envelope$1_$reflection(gen0) {
    return record_type("Channel.Types.Envelope`1", [gen0], Envelope$1, () => [["Id", int32_type], ["Ts", class_type("System.DateTime")], ["Content", gen0]]);
}

export class ChannelInfo extends Record {
    constructor(Id, Name, Topic, UserCount) {
        super();
        this.Id = Id;
        this.Name = Name;
        this.Topic = Topic;
        this.UserCount = (UserCount | 0);
    }
}

export function ChannelInfo_$reflection() {
    return record_type("Channel.Types.ChannelInfo", [], ChannelInfo, () => [["Id", string_type], ["Name", string_type], ["Topic", string_type], ["UserCount", int32_type]]);
}

export function ChannelInfo_get_Empty() {
    return new ChannelInfo(defaultOf(), defaultOf(), "", 0);
}

export class ChannelData extends Record {
    constructor(Info, Users, Messages, PostText) {
        super();
        this.Info = Info;
        this.Users = Users;
        this.Messages = Messages;
        this.PostText = PostText;
    }
}

export function ChannelData_$reflection() {
    return record_type("Channel.Types.ChannelData", [], ChannelData, () => [["Info", ChannelInfo_$reflection()], ["Users", class_type("Microsoft.FSharp.Collections.FSharpMap`2", [string_type, UserInfo_$reflection()])], ["Messages", list_type(Envelope$1_$reflection(Message_$reflection()))], ["PostText", string_type]]);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Init", "Update", "AppendMessage", "AppendUserMessage", "UserJoined", "UserLeft", "UserUpdated", "SetPostText", "PostText", "Forward", "Leave"];
    }
}

export function Msg_$reflection() {
    return union_type("Channel.Types.Msg", [], Msg, () => [[["Item1", ChannelInfo_$reflection()], ["Item2", list_type(UserInfo_$reflection())], ["Item3", list_type(tuple_type(string_type, Envelope$1_$reflection(string_type)))]], [["Item", ChannelInfo_$reflection()]], [["Item", Envelope$1_$reflection(Message_$reflection())]], [["Item1", string_type], ["Item2", Envelope$1_$reflection(string_type)]], [["Item", UserInfo_$reflection()]], [["Item", string_type]], [["Item", UserInfo_$reflection()]], [["Item", string_type]], [], [["Item", string_type]], []]);
}

