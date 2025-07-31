using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoAVL.Drawables;
using UltraDES;

namespace AutoAVL
{
    public class Automaton
    {
        private DeterministicFiniteAutomaton? dfa;
        private NondeterministicFiniteAutomaton? ndfa;

        private string automatonName;

        public Automaton(DeterministicFiniteAutomaton automaton)
        {
            this.dfa = automaton;
        }

        public Automaton(NondeterministicFiniteAutomaton automaton)
        {
            this.ndfa = automaton;
        }

        public Automaton()
        {
            dfa = new DeterministicFiniteAutomaton(new List<Transition>(), new State("", Marking.Unmarked), "");
        }

        public List<AbstractState> States()
        {
            return (dfa == null) ? ndfa.States.ToList() : dfa.States.ToList();
        }

        public List<Transition> Transitions()
        {
            return (dfa == null) ? ndfa.Transitions.ToList() : dfa.Transitions.ToList();
        }

        public string InitialState()
        {
            return (dfa == null) ? ndfa.InitialState.ToString() : dfa.InitialState.ToString();
        }

        public DeterministicFiniteAutomaton GetDFA()
        {
            return dfa;
        }

        public void UpdateAutomaton(List<Node> nodes, List<Link> links)
        {

            // Dicionário para armazenar States, usando o Alias como chave para busca eficiente
            Dictionary<string, State> statesDict = new Dictionary<string, State>();
            // Dicionário para armazenar Events, usando o Alias como chave para garantir unicidade
            Dictionary<string, Event> eventsDict = new Dictionary<string, Event>();

            State initialState = null; // O estado inicial do autômato

            // --- Passo 1: Criar e Coletar Estados ---
            foreach (Node node in nodes)
            {
                // Garante que o Estado seja criado e adicionado ao dicionário.
                // Se um nó com o mesmo nome existir, ele será atualizado (se Alias for a chave).
                // Se a classe State já tem Equals/GetHashCode por Alias, Dictionary.Add() lança erro se já existir.
                // Uma abordagem mais robusta seria verificar antes de adicionar:
                string stateAlias = node.name;
                if (!statesDict.ContainsKey(stateAlias))
                {
                    statesDict.Add(stateAlias, new State(stateAlias, node.marked ? Marking.Marked : Marking.Unmarked));
                }
                else
                {
                    
                }
            }

            // --- Passo 2: Coletar Eventos Únicos e Identificar o Estado Inicial ---
            foreach (Link link in links)
            {
                if (link.isInitialLink)
                {
                    statesDict.TryGetValue(link.end.name, out State foundInitialState);
                    initialState = foundInitialState;
                }
                else
                {
                    string eventAlias = link.name;
                    if (!eventsDict.ContainsKey(eventAlias))
                    {
                        eventsDict.Add(eventAlias, new Event(eventAlias, Controllability.Controllable));
                    }
                }
            }


            // --- Passo 3: Criar Transições (incluindo autoLinks) ---
            List<Transition> transitions = new List<Transition>();
            foreach (Link link in links)
            {
                if (link.isInitialLink)
                {
                    continue;
                }

                State origin = statesDict.GetValueOrDefault(link.start.name);
                State destination = statesDict.GetValueOrDefault(link.end.name);
                Event trigger = eventsDict.GetValueOrDefault(link.name);

                transitions.Add(new Transition(origin, trigger, destination));
            }

            try
            {
                dfa = new DeterministicFiniteAutomaton(transitions, initialState, automatonName);
            }
            catch (Exception e)
            {
                ndfa = new NondeterministicFiniteAutomaton(transitions, initialState, automatonName);
            }
        }

        public void NewInitialState(string newInitialState)
        {
            if (dfa == null)
            {
                List<Transition> transitions = ndfa.Transitions.ToList();
                AbstractState formerInitialState = ndfa.InitialState;
                State initialState = new State(newInitialState, formerInitialState.Marking);
                string name = ndfa.Name;
                NondeterministicFiniteAutomaton newNDFA = new NondeterministicFiniteAutomaton(transitions, initialState, name);
                ndfa = newNDFA;
            }
            else
            {
                List<Transition> transitions = dfa.Transitions.ToList();
                AbstractState initialState = dfa.States.ToList().Find(x => x.ToString() == newInitialState);
                dfa = new DeterministicFiniteAutomaton(transitions, initialState, dfa.Name);
            }
        }

        public void SetName(string newName)
        {
            List<Transition> transitions = dfa.Transitions.ToList();
            AbstractState initialState = dfa.InitialState;
            dfa = new DeterministicFiniteAutomaton(transitions, initialState, newName);
        }
    }
}
