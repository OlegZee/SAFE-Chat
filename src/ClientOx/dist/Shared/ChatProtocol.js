import { Union, Record } from "../fable_modules/fable-library-js.4.25.0/Types.js";
import { union_type, option_type, list_type, bool_type, record_type, string_type, class_type, int32_type } from "../fable_modules/fable-library-js.4.25.0/Reflection.js";

export class ChannelMessageInfo extends Record {
    constructor(id, ts, text, chan, author) {
        super();
        this.id = (id | 0);
        this.ts = ts;
        this.text = text;
        this.chan = chan;
        this.author = author;
    }
}

export function ChannelMessageInfo_$reflection() {
    return record_type("FsChat.Protocol.ChannelMessageInfo", [], ChannelMessageInfo, () => [["id", int32_type], ["ts", class_type("System.DateTime")], ["text", string_type], ["chan", string_type], ["author", string_type]]);
}

export class ChanUserInfo extends Record {
    constructor(id, nick, isbot, status, email, imageUrl) {
        super();
        this.id = id;
        this.nick = nick;
        this.isbot = isbot;
        this.status = status;
        this.email = email;
        this.imageUrl = imageUrl;
    }
}

export function ChanUserInfo_$reflection() {
    return record_type("FsChat.Protocol.ChanUserInfo", [], ChanUserInfo, () => [["id", string_type], ["nick", string_type], ["isbot", bool_type], ["status", string_type], ["email", string_type], ["imageUrl", string_type]]);
}

export class ChannelInfo extends Record {
    constructor(id, name, userCount, topic) {
        super();
        this.id = id;
        this.name = name;
        this.userCount = (userCount | 0);
        this.topic = topic;
    }
}

export function ChannelInfo_$reflection() {
    return record_type("FsChat.Protocol.ChannelInfo", [], ChannelInfo, () => [["id", string_type], ["name", string_type], ["userCount", int32_type], ["topic", string_type]]);
}

export class ActiveChannelData extends Record {
    constructor(channelId, users, messageCount, unreadMessageCount, lastMessages) {
        super();
        this.channelId = channelId;
        this.users = users;
        this.messageCount = (messageCount | 0);
        this.unreadMessageCount = unreadMessageCount;
        this.lastMessages = lastMessages;
    }
}

export function ActiveChannelData_$reflection() {
    return record_type("FsChat.Protocol.ActiveChannelData", [], ActiveChannelData, () => [["channelId", string_type], ["users", list_type(ChanUserInfo_$reflection())], ["messageCount", int32_type], ["unreadMessageCount", option_type(int32_type)], ["lastMessages", list_type(ChannelMessageInfo_$reflection())]]);
}

export class UserMessageData extends Record {
    constructor(text, chan) {
        super();
        this.text = text;
        this.chan = chan;
    }
}

export function UserMessageData_$reflection() {
    return record_type("FsChat.Protocol.UserMessageData", [], UserMessageData, () => [["text", string_type], ["chan", string_type]]);
}

export class UserCommandData extends Record {
    constructor(command, chan) {
        super();
        this.command = command;
        this.chan = chan;
    }
}

export function UserCommandData_$reflection() {
    return record_type("FsChat.Protocol.UserCommandData", [], UserCommandData, () => [["command", string_type], ["chan", string_type]]);
}

export class ServerCommand extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UserCommand", "Join", "JoinOrCreate", "Leave", "Ping"];
    }
}

export function ServerCommand_$reflection() {
    return union_type("FsChat.Protocol.ServerCommand", [], ServerCommand, () => [[["Item", UserCommandData_$reflection()]], [["Item", string_type]], [["channelName", string_type]], [["Item", string_type]], []]);
}

export class ServerMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Greets", "UserMessage", "ServerCommand"];
    }
}

export function ServerMsg_$reflection() {
    return union_type("FsChat.Protocol.ServerMsg", [], ServerMsg, () => [[], [["Item", UserMessageData_$reflection()]], [["reqId", string_type], ["message", ServerCommand_$reflection()]]]);
}

export class HelloInfo extends Record {
    constructor(me, channels) {
        super();
        this.me = me;
        this.channels = channels;
    }
}

export function HelloInfo_$reflection() {
    return record_type("FsChat.Protocol.HelloInfo", [], HelloInfo, () => [["me", ChanUserInfo_$reflection()], ["channels", list_type(ChannelInfo_$reflection())]]);
}

export class ClientErrMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["AuthFail", "CannotProcess"];
    }
}

export function ClientErrMsg_$reflection() {
    return union_type("FsChat.Protocol.ClientErrMsg", [], ClientErrMsg, () => [[["Item", string_type]], [["Item", string_type]]]);
}

export class ChannelEvent extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Joined", "Left", "Updated"];
    }
}

export function ChannelEvent_$reflection() {
    return union_type("FsChat.Protocol.ChannelEvent", [], ChannelEvent, () => [[["Item", ChanUserInfo_$reflection()]], [["Item", string_type]], [["Item", ChanUserInfo_$reflection()]]]);
}

export class ServerEvent extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["NewChannel", "RemoveChannel", "ChannelEvent", "JoinedChannel"];
    }
}

export function ServerEvent_$reflection() {
    return union_type("FsChat.Protocol.ServerEvent", [], ServerEvent, () => [[["Item", ChannelInfo_$reflection()]], [["Item", ChannelInfo_$reflection()]], [["Item1", string_type], ["Item2", ChannelEvent_$reflection()]], [["Item", ActiveChannelData_$reflection()]]]);
}

export class ServerEventInfo extends Record {
    constructor(id, ts, evt) {
        super();
        this.id = (id | 0);
        this.ts = ts;
        this.evt = evt;
    }
}

export function ServerEventInfo_$reflection() {
    return record_type("FsChat.Protocol.ServerEventInfo", [], ServerEventInfo, () => [["id", int32_type], ["ts", class_type("System.DateTime")], ["evt", ServerEvent_$reflection()]]);
}

export class CommandResponse extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Error", "UserUpdated", "JoinedChannel", "LeftChannel", "Pong"];
    }
}

export function CommandResponse_$reflection() {
    return union_type("FsChat.Protocol.CommandResponse", [], CommandResponse, () => [[["Item", ClientErrMsg_$reflection()]], [["Item", ChanUserInfo_$reflection()]], [["Item", ChannelInfo_$reflection()]], [["chanId", string_type]], []]);
}

export class ClientMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Hello", "CmdResponse", "ChanMsg", "ServerEvent"];
    }
}

export function ClientMsg_$reflection() {
    return union_type("FsChat.Protocol.ClientMsg", [], ClientMsg, () => [[["Item", HelloInfo_$reflection()]], [["reqId", string_type], ["reply", CommandResponse_$reflection()]], [["Item", ChannelMessageInfo_$reflection()]], [["Item", ServerEventInfo_$reflection()]]]);
}

