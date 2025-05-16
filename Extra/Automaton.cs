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
            List<State> states = new List<State>();
            List<Event> events = new List<Event>();
            List<Transition> transitions = new List<Transition>();

            State initialState = new State("", Marking.Marked);

            foreach (Node node in nodes)
            {
                states.Add(new State(node.name, node.marked ? Marking.Marked : Marking.Unmarked));
            }

            foreach (Link link in links)
            {
                if (link.isInitialLink)
                {
                    initialState = states.Find(x => x.Alias == link.end.name);
                } else
                {
                    events.Add(new Event(link.name, Controllability.Controllable));
                }
            }

            foreach (Link link in links)
            {
                if (link.isInitialLink)
                    continue;

                State origin = states.Find(x => x.Alias == link.start.name);
                State destination = states.Find(x => x.Alias == link.end.name);
                Event trigger = events.Find(x => x.Alias == link.name);
                transitions.Add(new Transition(origin, trigger, destination));
            }

            try
            {
                dfa = new DeterministicFiniteAutomaton(transitions, initialState, automatonName);
            } catch (Exception e)
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
