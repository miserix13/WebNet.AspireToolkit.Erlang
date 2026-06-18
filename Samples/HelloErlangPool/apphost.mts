// Aspire TypeScript AppHost for Gen_Server Worker Pool Sample
// Demonstrates a pool of gen_server workers managed by a supervisor

import { join, resolve } from 'node:path';
import { createBuilder } from '../.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const ertsHome = process.env.ERTS_HOME ?? process.env.ERLANG_HOME;
const sampleAppPath = resolve('.\\Samples\\HelloErlangPool');
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
    'hello-pool-app',
    erlangRuntime,
    sampleAppPath,
    'hello_pool',
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
                name: 'hello_pool_sup',
                kind: 'supervisor',
                description: 'Root supervisor for the worker pool.'
            },
            {
                name: 'hello_pool_manager',
                kind: 'gen_server',
                description: 'Pool manager tracking worker availability and task dispatch.'
            },
            {
                name: 'hello_pool_worker',
                kind: 'worker',
                description: 'Individual worker processes executing tasks from the pool.'
            }
        ],
        otel: {
            enabled: true,
            serviceName: 'hello-pool-app',
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
