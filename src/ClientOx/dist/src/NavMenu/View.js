import { Helpers_classList } from "../../fable_modules/Fable.React.10.0.0-alpha.1/Fable.React.Helpers.fs.js";
import * as react from "react";
import { keyValueList } from "../../fable_modules/fable-library-js.4.25.0/MapUtil.js";
import { HTMLAttr, DOMAttr } from "../../fable_modules/Fable.React.10.0.0-alpha.1/Fable.React.Props.fs.js";
import { equals } from "../../fable_modules/fable-library-js.4.25.0/Util.js";
import { Route } from "../Router.js";
import { AppMsg } from "../Chat/Types.js";
import { empty, collect, singleton, append, delay, toList } from "../../fable_modules/fable-library-js.4.25.0/Seq.js";
import { root } from "../UserAvatar/View.js";
import { containsKey, toSeq } from "../../fable_modules/fable-library-js.4.25.0/Map.js";
import { singleton as singleton_1 } from "../../fable_modules/fable-library-js.4.25.0/List.js";

export function menuItem(htmlProp, name, topic, isCurrent) {
    const props_4 = [Helpers_classList([["btn", true], ["fs-channel", true], ["selected", isCurrent]]), htmlProp];
    const children_4 = [react.createElement("h1", {}, name), react.createElement("span", {}, topic)];
    return react.createElement("button", keyValueList(props_4, 1), ...children_4);
}

export function menuItemChannel(ch, currentPage) {
    return menuItem(new DOMAttr(40, [(_arg) => {
        throw 1;
    }]), ch.Name, ch.Topic, equals(new Route(1, [ch.Id]), currentPage));
}

export function menuItemChannelJoin(dispatch) {
    return (ch) => menuItem(new DOMAttr(40, [(_arg) => {
        dispatch(new AppMsg(4, [ch.Id]));
    }]), ch.Name, ch.Topic, false);
}

export function menu(chatData, currentPage, dispatch) {
    if (chatData.tag === 1) {
        const me = chatData.fields[0];
        const chat = chatData.fields[1];
        let patternInput;
        const _arg = chat.NewChanName;
        patternInput = ((_arg == null) ? [false, ""] : [true, _arg]);
        const opened = patternInput[0];
        return toList(delay(() => {
            let children_10, children_8;
            return append(singleton((children_10 = [root(me.ImageUrl), react.createElement("h3", {
                id: "usernick",
            }, me.Nick), react.createElement("span", {
                id: "userstatus",
            }, me.Status), (children_8 = [react.createElement("i", {
                className: "mdi mdi-logout-variant",
            })], react.createElement("button", {
                id: "logout",
                className: "btn",
                title: "Logout",
                onClick: (_arg_1) => {
                    throw 1;
                },
            }, ...children_8))], react.createElement("div", {
                className: "fs-user",
            }, ...children_10))), delay(() => {
                let children_16, children_14, props_12;
                return append(singleton((children_16 = ["My Channels", (children_14 = [(props_12 = [Helpers_classList([["mdi", true], ["mdi-close", opened], ["mdi-plus", !opened]])], react.createElement("i", keyValueList(props_12, 1)))], react.createElement("button", {
                    className: "btn",
                    title: "Create New",
                    onClick: (_arg_2) => {
                        dispatch(new AppMsg(2, [opened ? undefined : ""]));
                    },
                }, ...children_14))], react.createElement("h2", {}, ...children_16))), delay(() => {
                    let props_18;
                    return append(singleton((props_18 = [new HTMLAttr(159, ["text"]), Helpers_classList([["fs-new-channel", true], ["open", opened]]), new HTMLAttr(128, ["Type the channel name here..."]), new HTMLAttr(1, [patternInput[1]]), new HTMLAttr(55, [true]), new DOMAttr(9, [(ev) => {
                        dispatch(new AppMsg(2, [ev.target.value]));
                    }]), new DOMAttr(16, [(ev_1) => {
                        if ((ev_1.which === 13) ? true : (ev_1.keyCode === 13)) {
                            dispatch(new AppMsg(3, []));
                        }
                    }])], react.createElement("input", keyValueList(props_18, 1)))), delay(() => append(collect((matchValue) => singleton(menuItemChannel(matchValue[1].Info, currentPage)), toSeq(chat.Channels)), delay(() => {
                        let children_22, children_20;
                        return append(singleton((children_22 = ["All Channels", (children_20 = [react.createElement("i", {
                            className: "mdi mdi-magnify",
                        })], react.createElement("button", {
                            className: "btn",
                            title: "Search",
                        }, ...children_20))], react.createElement("h2", {}, ...children_22))), delay(() => collect((matchValue_1) => (!containsKey(matchValue_1[0], chat.Channels) ? singleton(menuItemChannelJoin(dispatch)(matchValue_1[1])) : empty()), toSeq(chat.ChannelList))));
                    }))));
                }));
            }));
        }));
    }
    else {
        return singleton_1(react.createElement("div", {}, "not connected"));
    }
}

