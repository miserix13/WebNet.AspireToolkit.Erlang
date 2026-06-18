// Aspire TypeScript AppHost - Multiple Erlang OTP Sample Selector
// For more information, see: https://aspire.dev
//
// Available samples (set via ASPIRE_SAMPLE environment variable):
//   - hello_rebar3 (default): Basic Erlang app with supervisor and gen_server
//   - hello_pool: Gen_server worker pool pattern with task distribution
//   - hello_statem: Gen_statem finite state machine pattern with state transitions

import { join, resolve } from 'node:path';
import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();
const ertsHome = process.env.ERTS_HOME ?? process.env.ERLANG_HOME;

const sampleName = process.env.ASPIRE_SAMPLE ?? 'hello_rebar3';
const sampleMap: { [key: string]: { path: string; app: string; description: string; monitored: any[] } } = {
    hello_rebar3: {
        path: '.\\Samples\\HelloErlangRebar3',
        app: 'hello_erlang',
        description: 'Basic Erlang app with supervisor and gen_server',
        monitored: [
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
        ]
    },
    hello_pool: {
        path: '.\\Samples\\HelloErlangPool',
        app: 'hello_pool',
        description: 'Gen_server worker pool pattern for task distribution',
        monitored: [
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
        ]
    },
    hello_statem: {
        path: '.\\Samples\\HelloErlangStatem',
        app: 'hello_statem',
        description: 'Gen_statem finite state machine pattern with state transitions',
        monitored: [
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
        ]
    }
};

const sample = sampleMap[sampleName];
if (!sample) {
    throw new Error(`Unknown sample: ${sampleName}. Available samples: ${Object.keys(sampleMap).join(', ')}`);
}

if (!ertsHome) {
    throw new Error('Set ERTS_HOME (or ERLANG_HOME) before starting the AppHost.');
}

const sampleAppPath = resolve(sample.path);
const rebar3Path = process.env.REBAR3_PATH ?? join(sampleAppPath, 'tools', 'rebar3.cmd');

console.log(`[AppHost] Loading sample: ${sampleName} (${sample.description})`);

const erlangRuntime = await builder.addErts('erlang-runtime', ertsHome, {
    enableRuntimePackageCommands: true
})
    .withPersistentLifetime()
    .withRequiredCommand('erl');

await builder.addErlangApp(
    `${sampleName}-app`,
    erlangRuntime,
    sampleAppPath,
    sample.app,
    {
        rebar3ExecutablePath: rebar3Path,
        profile: 'default',
        runCommand: 'shell',
        enableHexCommands: true,
        hexDependencyArguments: ['--verbose'],
        environmentVariables: {
            ERL_FLAGS: '+S 2:2'
        },
        monitoredProcesses: sample.monitored,
        otel: {
            enabled: true,
            serviceName: `${sampleName}-app`,
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