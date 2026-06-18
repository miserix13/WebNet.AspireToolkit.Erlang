// Aspire TypeScript AppHost for Gen_Statem State Machine Sample
// Demonstrates OTP finite state machines (FSM) with state transitions

import { join, resolve } from 'node:path';
import { createBuilder } from '../.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const ertsHome = process.env.ERTS_HOME ?? process.env.ERLANG_HOME;
const sampleAppPath = resolve('.\\Samples\\HelloErlangStatem');
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
    'hello-statem-app',
    erlangRuntime,
    sampleAppPath,
    'hello_statem',
    {
        rebar3ExecutablePath: rebar3Path,
        profile: 'default',
        runCommand: 'shell',
        enableHexCommands: true,
        environmentVariables: {
            ERL_FLAGS: '+S 2:2'
        },
        monitoredProcesses: [
            {
                name: 'hello_statem_sup',
                kind: 'supervisor',
                description: 'Root supervisor for state machine instances.'
            },
            {
                name: 'hello_statem_registry',
                kind: 'gen_server',
                description: 'Registry tracking all active state machine instances and their current states.'
            },
            {
                name: 'hello_statem_instance',
                kind: 'gen_statem',
                description: 'Individual finite state machine instances executing state transitions.'
            }
        ],
        otel: {
            enabled: true,
            serviceName: 'hello-statem-app',
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
