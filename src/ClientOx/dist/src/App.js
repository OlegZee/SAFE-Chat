import * as app from "../../sass/app.scss";
import { menu } from "./NavMenu/View.js";
import { Msg } from "./Types.js";
import * as react from "react";
import { FSharpMap__get_Item, containsKey } from "../fable_modules/fable-library-js.4.25.0/Map.js";
import { root as root_1 } from "./Channel/View.js";
import { AppMsg } from "./Chat/Types.js";
import { singleton } from "../fable_modules/fable-library-js.4.25.0/List.js";
import { root as root_2 } from "./Overview/View.js";
import { ProgramModule_mkProgram, ProgramModule_run } from "../fable_modules/Fable.Elmish.5.0.0/program.fs.js";
import { Program_withReactSynchronous } from "../fable_modules/Fable.Elmish.React.5.0.0/react.fs.js";
import { ProgramModule_toNavigable } from "../fable_modules/Fable.Elmish.Browser.4.1.0/navigation.fs.js";
import { parseHash } from "../fable_modules/Fable.Elmish.Browser.4.1.0/parser.fs.js";
import { update, init, urlUpdate as urlUpdate_1 } from "./State.js";


export function root(model, dispatch) {
    let children_2, f1_4, f1_3, children_4, _arg, chan, matchValue, f1_2, f1_1, f2;
    const children_6 = [(children_2 = menu(model.chat, model.currentPage, (f1_4 = ((f1_3 = (() => {
        throw 1;
    })(), (arg_3) => (new Msg(f1_3(arg_3))))), (arg_4) => {
        dispatch(f1_4(arg_4));
    })), react.createElement("div", {
        className: "col-md-4 fs-menu",
    }, ...children_2)), (children_4 = ((_arg = model.currentPage, (_arg.tag === 1) ? ((chan = _arg.fields[0], (matchValue = model.chat, (matchValue.tag === 1) ? (containsKey(chan, matchValue.fields[1].Channels) ? root_1(FSharpMap__get_Item(matchValue.fields[1].Channels, chan), (f1_2 = ((f1_1 = ((f2 = (() => {
        throw 1;
    })(), (arg) => f2(new AppMsg(1, [chan, arg])))), (arg_1) => (new Msg(f1_1(arg_1))))), (arg_2) => {
        dispatch(f1_2(arg_2));
    })) : singleton(react.createElement("div", {}, "bad channel route"))) : singleton(react.createElement("div", {}, "bad channel route"))))) : singleton(root_2))), react.createElement("div", {
        className: "col-xs-12 col-md-8 fs-chat",
    }, ...children_4))];
    return react.createElement("div", {
        className: "container",
    }, ...children_6);
}

ProgramModule_run(Program_withReactSynchronous("elmish-app", ProgramModule_toNavigable((() => {
    let parser;
    throw 1;
    return (location) => parseHash(parser, location);
})(), urlUpdate_1, ProgramModule_mkProgram(init, update, root))));

