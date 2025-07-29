import { remove, add, tryFind, ofList, empty } from "../../fable_modules/fable-library-js.4.25.0/Map.js";
import { comparePrimitives } from "../../fable_modules/fable-library-js.4.25.0/Util.js";
import { Msg, Envelope$1, Message, UserInfo, ChannelData, ChannelInfo_get_Empty } from "./Types.js";
import { singleton, append, map, empty as empty_1 } from "../../fable_modules/fable-library-js.4.25.0/List.js";
import { Cmd_none } from "../../fable_modules/Fable.Elmish.5.0.0/cmd.fs.js";
import { defaultArgWith, map as map_1 } from "../../fable_modules/fable-library-js.4.25.0/Option.js";
import { now } from "../../fable_modules/fable-library-js.4.25.0/Date.js";
import { toConsole, printf, toText } from "../../fable_modules/fable-library-js.4.25.0/String.js";

export function init() {
    let Users;
    return [(Users = empty({
        Compare: comparePrimitives,
    }), new ChannelData(ChannelInfo_get_Empty(), Users, empty_1(), "")), Cmd_none()];
}

export function init2(chan, users) {
    let bind$0040;
    return [(bind$0040 = init()[0], new ChannelData(chan, ofList(map((u) => [u.Id, u], users), {
        Compare: comparePrimitives,
    }), bind$0040.Messages, bind$0040.PostText)), Cmd_none()];
}

export function getUserNick(userid, users) {
    return map_1((user) => user.Nick, tryFind(userid, users));
}

export function unknownUser(userId, unitVar) {
    return new UserInfo(userId, "Unknown #" + userId, "", false, true, undefined, false);
}

export function mapUser(users, userId) {
    return defaultArgWith(tryFind(userId, users), () => unknownUser(userId, undefined));
}

export function mapMessage(_arg, author) {
    return new Envelope$1(_arg.Id, _arg.Ts, new Message(0, [_arg.Content, author]));
}

export function update(msg, state) {
    let Messages_2, Messages_3, _arg, oldnick_1, txt, Messages_4, _arg_1, oldnick_2, txt_1;
    switch (msg.tag) {
        case 1:
            return [new ChannelData(msg.fields[0], state.Users, state.Messages, state.PostText), Cmd_none()];
        case 2:
            return [new ChannelData(state.Info, state.Users, append(state.Messages, singleton(msg.fields[0])), state.PostText), Cmd_none()];
        case 3:
            return [new ChannelData(state.Info, state.Users, append(state.Messages, singleton(mapMessage(msg.fields[1], mapUser(state.Users, msg.fields[0])))), state.PostText), Cmd_none()];
        case 4: {
            const user = msg.fields[0];
            return [(Messages_2 = append(state.Messages, singleton(new Envelope$1(0, now(), new Message(1, [toText(printf("%s joined the channel"))(user.Nick)])))), new ChannelData(state.Info, add(user.Id, user, state.Users), Messages_2, state.PostText)), Cmd_none()];
        }
        case 6: {
            const user_1 = msg.fields[0];
            return [(Messages_3 = append(state.Messages, (_arg = getUserNick(user_1.Id, state.Users), (_arg != null) ? ((_arg !== user_1.Nick) ? ((oldnick_1 = _arg, (txt = toText(printf("%s is now known as %s"))(oldnick_1)(user_1.Nick), singleton(new Envelope$1(0, now(), new Message(1, [txt])))))) : empty_1()) : empty_1())), new ChannelData(state.Info, add(user_1.Id, user_1, state.Users), Messages_3, state.PostText)), Cmd_none()];
        }
        case 5: {
            const userId_1 = msg.fields[0];
            return [(Messages_4 = append(state.Messages, (_arg_1 = getUserNick(userId_1, state.Users), (_arg_1 != null) ? ((oldnick_2 = _arg_1, (txt_1 = toText(printf("%s left the channel"))(oldnick_2), singleton(new Envelope$1(0, now(), new Message(1, [txt_1])))))) : empty_1())), new ChannelData(state.Info, remove(userId_1, state.Users), Messages_4, state.PostText)), Cmd_none()];
        }
        case 7:
            return [new ChannelData(state.Info, state.Users, state.Messages, msg.fields[0]), Cmd_none()];
        case 8: {
            const matchValue = state.PostText;
            if (matchValue.trim() !== "") {
                return [new ChannelData(state.Info, state.Users, state.Messages, ""), singleton((dispatch) => {
                    dispatch(new Msg(9, [matchValue]));
                })];
            }
            else {
                return [state, Cmd_none()];
            }
        }
        case 10:
        case 9: {
            toConsole(printf("%A message is not expected in channel update."))(msg);
            return [state, Cmd_none()];
        }
        default: {
            const users = ofList(map((u) => [u.Id, u], msg.fields[1]), {
                Compare: comparePrimitives,
            });
            return [new ChannelData(msg.fields[0], users, map((tupledArg) => mapMessage(tupledArg[1], mapUser(users, tupledArg[0])), msg.fields[2]), ""), Cmd_none()];
        }
    }
}

