// Aspire TypeScript AppHost
// For more information, see: https://aspire.dev

import { join, resolve } from 'node:path';
import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const ertsHome = process.env.ERTS_HOME ?? process.env.ERLANG_HOME;
const sampleAppPath = resolve('.\\Samples\\HelloErlangRebar3');
const rebar3Path = process.env.REBAR3_PATH ?? join(sampleAppPath, 'tools', 'rebar3.cmd');

if (!ertsHome) {
    throw new Error('Set ERTS_HOME (or ERLANG_HOME) before starting the AppHost.');
}

const erlangRuntime = await builder.addErts('erlang-runtime', ertsHome, {
    enableRuntimePackageCommands: true
})
    .withPersistentLifetime()
    .withRequiredCommand('erl');

await builder.addErlangApp(
    'hello-erlang-app',
    erlangRuntime,
    sampleAppPath,
    'hello_erlang',
    {
        rebar3ExecutablePath: rebar3Path,
        profile: 'default',
        runCommand: 'shell',
        environmentVariables: {
            ERL_FLAGS: '+S 2:2'
        },
        monitoredProcesses: [
            {
                name: 'hello_erlang_sup',
                kind: 'supervisor',
                description: 'Top-level OTP supervisor for the sample app.'
            },
            {
                name: 'hello_erlang_server',
                kind: 'worker',
                description: 'Heartbeat gen_server used by the sample app.'
            }
        ],
        otel: {
            enabled: true,
            serviceName: 'hello-erlang-app',
            resourceAttributes: {
                'service.namespace': 'samples',
                'service.language': 'erlang'
            }
        }
    })
    .waitFor(erlangRuntime)
    .withPersistentLifetime()
    .withRequiredCommand(rebar3Path);

await builder.build().run();