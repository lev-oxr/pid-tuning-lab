import * as Comlink from 'comlink'

type EngineExports = {
  Ping(): string
  Add(a: number, b: number): number
  Echo(s: string): string
  Busy(ms: number): void
}
let engine: EngineExports | null = null

async function init() {
  if (engine) return
  const url = new URL(
    `${import.meta.env.BASE_URL}dotnet/_framework/dotnet.js`,
    self.location.origin,
  ).href
  const { dotnet } = await import(/* @vite-ignore */ url)
  const { getAssemblyExports, getConfig } = await dotnet.create()
  const exports = await getAssemblyExports(getConfig().mainAssemblyName)
  engine = exports.PidLab.Wasm.Engine as EngineExports
}

const api = {
  ping: async () => {
    await init()
    return engine!.Ping()
  },
  add: async (a: number, b: number) => {
    await init()
    return engine!.Add(a, b)
  },
  echo: async (s: string) => {
    await init()
    return engine!.Echo(s)
  },
  busy: async (ms: number) => {
    await init()
    engine!.Busy(ms)
  },
}
export type EngineApi = typeof api
Comlink.expose(api)
