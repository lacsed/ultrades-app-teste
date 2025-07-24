using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using AutoAVL.Settings;
using UltraDES;
using System.IO;

namespace AutoAVL.Drawables
{
    public class Graph
    {
        public List<Node>? graphNodes;
        public List<Link>? graphLinks;

        public PhyD phyD;
        public DrawingDir drawingDir;
        public SvgCanvas svgCanvas;

        public Automaton _automaton;

        public bool firstSimulation;
        
        public Graph()
        {
            _automaton = new Automaton();
            graphNodes = new List<Node>();
            graphLinks = new List<Link>();

            phyD = new PhyD();
            drawingDir = new DrawingDir();

            svgCanvas = new SvgCanvas();
            
            firstSimulation = false;
        }
        
        public Graph(Automaton automaton)
        {
            _automaton = automaton;
            graphNodes = new List<Node>();
            graphLinks = new List<Link>();

            phyD = new PhyD();
            drawingDir = new DrawingDir();

            svgCanvas = new SvgCanvas();

            firstSimulation = true;

            foreach (AbstractState state in automaton.States())
            {
                graphNodes.Add(new Node(state));
            }

            foreach (Transition transition in automaton.Transitions())
            {
                graphLinks.Add(new Link(graphNodes.Find(x => x.name == transition.Origin.ToString()),
                    graphNodes.Find(x => x.name == transition.Destination.ToString()), transition.Trigger.ToString()));
            }
            graphLinks.Add(new Link(graphNodes.Find(x => x.name == automaton.InitialState())));
        }

        public void AddNode(Vector2D position)
        {
            graphNodes.Add(new Node(position, "", false));
        }

        public void ChangeInitialState(string newInitialState)
        {
            _automaton.NewInitialState(newInitialState);
        }

        public void AddLink(Link newLink)
        {
            AbstractState initialState = _automaton.States().Find(x => x.ToString() == newLink.start.name);
            AbstractState finalState = _automaton.States().Find(x => x.ToString() == newLink.end.name);

            Event linkEvent = new Event("", Controllability.Controllable);

            Transition linkTransition = new Transition(initialState, linkEvent, finalState);

            graphLinks.Add(newLink);
            _automaton.UpdateAutomaton(graphNodes, graphLinks);
        }

        public void AddInitialLink(Link newLink)
        {
            Link previousInitialLink = graphLinks.Find(x => x.isInitialLink);

            if (previousInitialLink != null)
            {
                Console.WriteLine("Anterior inicial = " + previousInitialLink.name);
                graphLinks.Remove(previousInitialLink);
            }

            Console.WriteLine("links antes");
            foreach (Link link in graphLinks)
                Console.WriteLine("nome do link = " + link.name);
            Console.WriteLine("links depois");
            foreach (Link link in graphLinks)
                Console.WriteLine("nome do link = " + link.name);
            Console.WriteLine("Fim");
            _automaton.UpdateAutomaton(graphNodes, graphLinks);
            Console.WriteLine("Número de transições depois de recriar = " + _automaton.Transitions().Count);
            graphLinks.Add(newLink);
        }

        public void AddAutoLink(Link newLink)
        {
            Link existingAutoLink = graphLinks.Find(x => x.isAutoLink == true && x.start == newLink.start);

            if (existingAutoLink != null)
            {
                existingAutoLink._directionAuxiliary = newLink._directionAuxiliary;
            }
            else
            {
                graphLinks.Add(newLink);
            }

            _automaton.UpdateAutomaton(graphNodes, graphLinks);
        }

        public void UpdateAutomaton()
        {
            _automaton.UpdateAutomaton(graphNodes, graphLinks);
        }

        public void Simulate()
        {
            if (firstSimulation)
            {
                Node.InitialPositioning(graphNodes);
                firstSimulation = false;
            }

            double maxDisplacement;

            int numero = 0;
            do
            {
                Node.ResetDisplacement(graphNodes);
                Node.InteractNodes(graphNodes, phyD);
                Link.PullLinks(graphLinks, phyD);

                numero++;
                maxDisplacement = Node.DisplaceNodes(graphNodes);

            } while (maxDisplacement > phyD.stopCondition);

            /* do
            {
                Node.ResetDisplacement(graphNodes);
                Node.InteractNodes(graphNodes, phyD);
                Link.PullLinks(graphLinks, phyD);
                Node.VerticalForce(graphNodes);

                maxDisplacement = Node.DisplaceNodes(graphNodes);

            } while (maxDisplacement > phyD.stopCondition); */

            Link.SetUp(graphLinks, drawingDir);
            AlignInitialTransition();
        }

        public void Shuffle()
        {
            double maxDisplacement = double.MaxValue;

            do
            {
                Node.ResetDisplacement(graphNodes);
                Node.InteractNodes(graphNodes, phyD);
                Link.PullLinks(graphLinks, phyD);

                maxDisplacement = Node.DisplaceNodes(graphNodes);

            } while (maxDisplacement > phyD.stopCondition);
        }
        
        public void AlignInitialTransition()
        {
            Link initialLink = graphLinks.Find(x => x.isInitialLink);
            Vector2D direction = initialLink.start.position - initialLink._directionAuxiliary;
            Vector2D positiveX = new Vector2D(1,0);
            double angle = direction.UnsignedAngle();
            
            foreach(Node node in graphNodes)
            {
                node.position.Rotate(-angle);
            }
            
            Link.SetUp(graphLinks, drawingDir);
        }

        public string ToSvg()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string svgImage = "";

            svgCanvas.SetUpCanvas(this.GetCanvasLimits());

            foreach (Drawable drawable in graphNodes.Concat<Drawable>(graphLinks).ToList())
                svgImage += drawable.ToSvg(drawingDir, svgCanvas);

            string svgDimensions = svgCanvas.SvgDimensions();
            string svgSettings = svgCanvas.SvgSettings();
            string svgHeading = @"<?xml version=""1.0"" standalone=""no""?>" + Environment.NewLine + @"<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""https://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">" + Environment.NewLine;

            return svgDimensions + svgSettings + svgImage + "</svg>";
        }

        public void AlignCanvas()
        {
            svgCanvas.SetUpCanvas(GetCanvasLimits());
        }

        public string ToLatex()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string latexDocument = @"\begin{tikzpicture}[scale=0.1]" + Environment.NewLine;
            latexDocument += "\\tikzstyle{every node}+=[inner sep=0pt]" + Environment.NewLine;

            foreach (Drawable drawable in graphNodes.Concat<Drawable>(graphLinks).ToList())
                latexDocument += drawable.ToLatex(drawingDir);

            latexDocument += @"\end{tikzpicture}" + Environment.NewLine;

            return latexDocument;
        }

        public Box GetCanvasLimits()
        {
            Box canvasBox = new Box();

            foreach (Drawable drawable in graphNodes.Concat<Drawable>(graphLinks).ToList())
            {
                canvasBox = Box.EncompassingBox(canvasBox, drawable.GetBox(drawingDir));
            }

            return canvasBox;
        }

        public void SaveSVG(string path, string fileName)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            StreamWriter sw = File.CreateText(path + fileName + ".svg");
            sw.Write(this.ToSvg());
            sw.Close();
        }

        public void SaveLatex(string path, string fileName)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            StreamWriter sw = File.CreateText(path + fileName + ".txt");
            sw.Write(this.ToLatex());
            sw.Close();
        }
    }
}

