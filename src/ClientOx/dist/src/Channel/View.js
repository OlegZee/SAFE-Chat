import { toShortDateString, toShortTimeString, now, op_Subtraction } from "../../fable_modules/fable-library-js.4.25.0/Date.js";
import { totalDays, totalHours, totalMinutes } from "../../fable_modules/fable-library-js.4.25.0/TimeSpan.js";
import { printf, toText } from "../../fable_modules/fable-library-js.4.25.0/String.js";
import { DOMAttr, Prop, HTMLAttr } from "../../fable_modules/Fable.React.10.0.0-alpha.1/Fable.React.Props.fs.js";
import { equals } from "../../fable_modules/fable-library-js.4.25.0/Util.js";
import { Msg } from "./Types.js";
import * as react from "react";
import { keyValueList } from "../../fable_modules/fable-library-js.4.25.0/MapUtil.js";
import { singleton as singleton_1, append, map, delay, toList } from "../../fable_modules/fable-library-js.4.25.0/Seq.js";
import { ofArray, singleton } from "../../fable_modules/fable-library-js.4.25.0/List.js";
import { Helpers_classList } from "../../fable_modules/Fable.React.10.0.0-alpha.1/Fable.React.Helpers.fs.js";
import { root as root_1 } from "../UserAvatar/View.js";

function formatTs(ts) {
    const matchValue = op_Subtraction(now(), ts);
    if (totalMinutes(matchValue) < 1) {
        return "a few seconds ago";
    }
    else if (totalMinutes(matchValue) < 30) {
        const arg = ~~totalMinutes(matchValue) | 0;
        return toText(printf("%i minutes ago"))(arg);
    }
    else if (totalHours(matchValue) <= 12) {
        return toShortTimeString(ts);
    }
    else if (totalDays(matchValue) <= 5) {
        const arg_1 = ~~totalDays(matchValue) | 0;
        return toText(printf("%i days ago"))(arg_1);
    }
    else {
        return toShortDateString(ts);
    }
}

export function messageInput(dispatch, model) {
    let props, value, children_2;
    const children_4 = [(props = [new HTMLAttr(159, ["text"]), new HTMLAttr(128, ["Type the message here..."]), (value = model.PostText, new Prop(1, [(e) => {
        if (!(e == null) && !equals(e.value, value)) {
            e.value = value;
        }
    }])), new DOMAttr(9, [(ev) => {
        dispatch(new Msg(7, [ev.target.value]));
    }]), new DOMAttr(16, [(ev_1) => {
        if ((ev_1.which === 13) ? true : (ev_1.keyCode === 13)) {
            dispatch(new Msg(8, []));
        }
    }])], react.createElement("input", keyValueList(props, 1))), (children_2 = [react.createElement("i", {
        className: "mdi mdi-send mdi-24px",
        onClick: (_arg) => {
            dispatch(new Msg(8, []));
        },
    })], react.createElement("button", {
        className: "btn",
    }, ...children_2))];
    return react.createElement("div", {
        className: "fs-message-input",
    }, ...children_4);
}

export function chanUsers(users) {
    let children_2;
    const children_4 = ["Users:", (children_2 = toList(delay(() => map((u_1) => {
        let u;
        const children = [(u = u_1[1], u.IsBot ? toText(printf("#%s"))(u.Nick) : u.Nick)];
        return react.createElement("li", {}, ...children);
    }, users))), react.createElement("ul", {}, ...children_2))];
    return react.createElement("div", {
        className: "userlist",
    }, ...children_4);
}

export function chatInfo(dispatch, model) {
    let children_6;
    const children_8 = [react.createElement("h1", {}, model.Info.Name), react.createElement("span", {}, model.Info.Topic), (children_6 = [react.createElement("i", {
        className: "mdi mdi-door-closed mdi-18px",
    })], react.createElement("button", {
        id: "leaveChannel",
        className: "btn",
        title: "Leave",
        onClick: (_arg) => {
            dispatch(new Msg(10, []));
        },
    }, ...children_6))];
    return react.createElement("div", {
        className: "fs-chat-info",
    }, ...children_8);
}

export function message(text) {
    return singleton(text);
}

export function messageList(messages) {
    const children_14 = toList(delay(() => map((m) => {
        let children_10, children_6;
        const matchValue = m.Content;
        if (matchValue.tag === 1) {
            const children_12 = [matchValue.fields[0], " ", (children_10 = [formatTs(m.Ts)], react.createElement("small", {}, ...children_10))];
            return react.createElement("blockquote", {
                className: "",
            }, ...children_12);
        }
        else {
            const user = matchValue.fields[1];
            const props_8 = [Helpers_classList([["fs-message", true], ["user", user.isMe]])];
            const children_8 = [(children_6 = toList(delay(() => append(message(matchValue.fields[0]), delay(() => {
                let children_4, children_2;
                return singleton_1((children_4 = [react.createElement("span", {
                    className: "user",
                }, user.Nick), (children_2 = [formatTs(m.Ts)], react.createElement("span", {
                    className: "time",
                }, ...children_2))], react.createElement("h5", {}, ...children_4)));
            })))), react.createElement("div", {}, ...children_6)), root_1(user.ImageUrl)];
            return react.createElement("div", keyValueList(props_8, 1), ...children_8);
        }
    }, messages)));
    return react.createElement("div", {
        className: "fs-messages",
    }, ...children_14);
}

export function root(model, dispatch) {
    return ofArray([chatInfo(dispatch, model), react.createElement("div", {
        className: "fs-splitter",
    }), messageList(model.Messages), react.createElement("div", {
        className: "fs-splitter",
    }), messageInput(dispatch, model)]);
}

