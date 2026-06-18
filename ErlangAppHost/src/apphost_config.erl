-module(apphost_config).

-export([load_config/1, validate_config/1, normalize_resource/1]).

%% @doc Load AppHost configuration from file
load_config(ConfigFile) ->
    io:format("[Config] Loading configuration from: ~s~n", [ConfigFile]),
    case file:read_file(ConfigFile) of
        {ok, Binary} ->
            case parse_config(Binary) of
                {ok, Config} ->
                    validate_config(Config);
                {error, Reason} ->
                    {error, {parse_error, Reason}}
            end;
        {error, Reason} ->
            {error, {file_error, Reason}}
    end.

%% @doc Validate AppHost configuration
validate_config({error, _} = Error) ->
    Error;

validate_config(Config) ->
    io:format("[Config] Validating configuration~n"),
    % Basic validation for now
    {ok, Config}.

%% @doc Normalize a resource configuration
normalize_resource(Resource) ->
    % Apply defaults and normalize paths
    Resource.

%% Private functions

parse_config(Binary) ->
    % For MVP, expect Erlang term format
    try
        % Convert to string for erl_eval
        String = binary_to_list(Binary),
        case erl_scan:string(String) of
            {ok, Tokens, _} ->
                case erl_parse:parse_term(Tokens) of
                    {ok, Term} ->
                        {ok, Term};
                    {error, Reason} ->
                        {error, {parse_error, Reason}}
                end;
            {error, Reason, _} ->
                {error, {tokenize_error, Reason}}
        end
    catch
        error:Reason ->
            {error, {parse_exception, Reason}}
    end.
