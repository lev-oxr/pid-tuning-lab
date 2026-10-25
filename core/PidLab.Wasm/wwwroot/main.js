import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig, runMain } = await dotnet.create()
const exports = await getAssemblyExports(getConfig().mainAssemblyName)
const Engine = exports.PidLab.Wasm.Engine

console.log(Engine.Ping(), Engine.Add(2, 3))
await runMain()
