-module(apphost_json_rpc).
-behaviour(gen_server).

-export([start_link/0, send_request/3, receive_response/1]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-record(state, {
    request_id = 0,
    pending_requests = #{}  % Map of request_id -> reply_to
}).

-define(JSON_RPC_VERSION, <<"2.0">>).

start_link() ->
    gen_server:start_link({local, ?MODULE}, ?MODULE, [], []).

%% @doc Send a JSON-RPC request and get a response
%% Returns {ok, Result} or {error, Error}
send_request(Method, Params, Timeout) ->
    gen_server:call(?MODULE, {send_request, Method, Params}, Timeout).

%% @doc Receive a JSON-RPC response (typically for callbacks)
receive_response(RequestId) ->
    gen_server:call(?MODULE, {receive_response, RequestId}).

init([]) ->
    io:format("[JSON-RPC] Starting JSON-RPC handler~n"),
    {ok, #state{}}.

handle_call({send_request, Method, Params}, _From, State) ->
    % Increment request ID
    NewReqId = State#state.request_id + 1,
    
    % Build JSON-RPC request
    Request = json_rpc_request(NewReqId, Method, Params),
    
    % Send to socket (async)
    case apphost_socket:send_json(Request) of
        ok ->
            % For now, return synchronously
            % In production, this would be async with callbacks
            Response = apphost_socket:recv_json_response(NewReqId, 5000),
            {reply, Response, State#state{request_id = NewReqId}};
        {error, Reason} ->
            {reply, {error, {send_failed, Reason}}, State}
    end;

handle_call({receive_response, RequestId}, _From, State) ->
    % Wait for a specific response
    Response = apphost_socket:recv_json_response(RequestId, 5000),
    {reply, Response, State};

handle_call(_Request, _From, State) ->
    {reply, ok, State}.

handle_cast(_Msg, State) ->
    {noreply, State}.

handle_info(_Info, State) ->
    {noreply, State}.

terminate(_Reason, _State) ->
    ok.

code_change(_OldVsn, State, _Extra) ->
    {ok, State}.

%% Private functions

json_rpc_request(Id, Method, Params) ->
    #{
        <<"jsonrpc">> => ?JSON_RPC_VERSION,
        <<"method">> => atom_to_binary(Method, utf8),
        <<"params">> => Params,
        <<"id">> => Id
    }.
