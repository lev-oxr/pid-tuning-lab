import { useState } from 'react'
import { engine } from './engine/client'

type State =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'ok'; text: string; ms: number }
  | { kind: 'error'; message: string }

export default function App() {
  const [state, setState] = useState<State>({ kind: 'idle' })

  async function run(job: () => Promise<string>) {
    setState({ kind: 'loading' })
    const t0 = performance.now()
    try {
      const text = await job()
      setState({ kind: 'ok', text, ms: performance.now() - t0 })
    } catch (e) {
      setState({
        kind: 'error',
        message: e instanceof Error ? e.message : String(e),
      })
    }
  }

  const busy = state.kind === 'loading'
  return (
    <main>
      <h1>PidLab · Ф0</h1>
      <button
        disabled={busy}
        onClick={() =>
          run(
            async () =>
              `${await engine.ping()} · 2+3=${await engine.add(2, 3)}`,
          )
        }
      >
        Викликати C#
      </button>{' '}
      <button disabled={busy} onClick={() => run(() => engine.echo('привіт'))}>
        Echo
      </button>{' '}
      <button
        disabled={busy}
        onClick={() =>
          run(async () => {
            await engine.busy(2000)
            return 'busy 2s done'
          })
        }
      >
        Busy 2s
      </button>
      <p>
        Повзунок (має рухатись під час Busy): <input type="range" />
      </p>
      <p role="status">
        {state.kind === 'idle' && '—'}
        {state.kind === 'loading' && 'loading…'}
        {state.kind === 'ok' && `ok: ${state.text} (${state.ms.toFixed(0)} ms)`}
        {state.kind === 'error' && `error: ${state.message}`}
      </p>
    </main>
  )
}
