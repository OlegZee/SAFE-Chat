var path = require("path");
var webpack = require("webpack");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

var isProduction = process.argv.indexOf("--mode=production") >= 0;
var port = process.env.SUAVE_FABLE_PORT || "8083";
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

module.exports = {
    mode: isProduction ? "production" : "development",
    devtool: isProduction ? false : "eval-source-map",
    entry: './dist/App.js',
    output: {
        filename: 'bundle.js',
        path: resolve('./public'),
        clean: true
    },
    resolve: {
        modules: [
            "node_modules", resolve("../../node_modules/")
        ]
    },
    devServer: {
        static: {
            directory: resolve('./public')
        },
        proxy: [
            {
                context: ['/api/socket'],
                target: 'ws://localhost:' + port,
                ws: true
            },
            {
                context: ['/api', '/', '/logon', '/logoff', '/logonfast'],
                target: 'http://localhost:' + port,
                changeOrigin: true
            }],
        hot: true,
        port: 8080
    },
    module: {
        rules: [
            {
                test: /\.scss$/,
                use: [
                    "style-loader",
                    "css-loader",
                    "sass-loader"
                ]
            }
        ]
    },
    plugins: isProduction ? [] : [
        new webpack.HotModuleReplacementPlugin()
    ]
};