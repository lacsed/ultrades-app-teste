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
        public string graphName;

        public Graph()
        {
            _automaton = new Automaton();
            graphNodes = new List<Node>();
            graphLinks = new List<Link>();

            phyD = new PhyD();
            drawingDir = new DrawingDir();

            svgCanvas = new SvgCanvas();

            firstSimulation = false;
            graphName = "";
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
            graphName = automaton.GetName();

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
            graphLinks.Add(newLink);
            _automaton.UpdateAutomaton(graphNodes, graphLinks);
        }

        public void AddInitialLink(Link newLink)
        {
            if (!newLink.isInitialLink)
            {
                return;
            }

            Link previousInitialLink = graphLinks.FirstOrDefault(link => link.isInitialLink);

            if (previousInitialLink != null)
            {
                graphLinks.Remove(previousInitialLink);
            }

            graphLinks.Add(newLink);

            _automaton.UpdateAutomaton(graphNodes, graphLinks);
        }

        public void AddAutoLink(Link newLink)
        {
            Link existingAutoLink = graphLinks.Find(x => x.isAutoLink && x.start == newLink.start);

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
            if (!this.AreAllNodesConnected())
            {
                throw new InvalidOperationException("Error: All the nodes must be connected in order to perform an auto layout");
            }

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
            Link inicial = graphLinks.Find(link => link.isInitialLink);
        }

        public void SimulateOnOneAxis()
        {
            if (!this.AreAllNodesConnected())
            {
                throw new InvalidOperationException("Error: All the nodes must be connected in order to perform an auto layout");
            }

            Node initialNode = graphLinks.Find(x => x.isInitialLink).start;

            List<Node> listSimulationNodes = [.. graphNodes];
            List<Link> listSimulationLinks = [.. graphLinks];

            int indexInitialNode = listSimulationNodes.FindIndex(x => x == initialNode);

            if (indexInitialNode != 0)
            {
                Node nodeInPosition = listSimulationNodes[0];

                listSimulationNodes[0] = initialNode;
                listSimulationNodes[indexInitialNode] = nodeInPosition;
            }

            Node.InitialPositioningUnidimentional(listSimulationNodes, new Vector2D(1, 0));

            int numberSwitchedNodes;

            do
            {
                Node.ResetDisplacement(listSimulationNodes);    
                Node.InteractNodes(listSimulationNodes, phyD);
                Link.PullLinks(listSimulationLinks, phyD);

                numberSwitchedNodes = Node.SwitchNodes(listSimulationNodes, new Vector2D(1,0));
            } while (numberSwitchedNodes > 0);
            
        }

        public bool AreAllNodesConnected()
        {
            if (graphNodes == null || graphNodes.Count == 0)
            {
                return true;
            }

            HashSet<Node> visitedNodes = new HashSet<Node>();

            Node startNode = graphNodes[0];

            Queue<Node> nodeQueue = new Queue<Node>();
            nodeQueue.Enqueue(startNode);
            visitedNodes.Add(startNode);

            while (nodeQueue.Count > 0)
            {
                Node currentNode = nodeQueue.Dequeue();

                foreach (Link link in graphLinks)
                {
                    Node neighbourNode = null;
                    if (link.start == currentNode)
                    {
                        neighbourNode = link.end;
                    }
                    else if (link.end == currentNode)
                    {
                        neighbourNode = link.start;
                    }

                    if (neighbourNode != null && !visitedNodes.Contains(neighbourNode))
                    {
                        visitedNodes.Add(neighbourNode);
                        nodeQueue.Enqueue(neighbourNode);
                    }
                }
            }

            return visitedNodes.Count == graphNodes.Count;
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
            Vector2D direction = initialLink._directionAuxiliary;
            Vector2D positiveX = new Vector2D(1,0);
            double angle = (-direction).UnsignedAngle();
            initialLink._directionAuxiliary.Rotate(angle);
            
            foreach (Node node in graphNodes)
            {
                node.position.Rotate(-angle);
            }
            
            Link.SetUp(graphLinks, drawingDir);
        }

        public string ToSvg()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string svgImage = "";

            Box limitesCanvas = this.GetCanvasLimits();
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
            Box limitBox = GetCanvasLimits();
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
            // Crie uma lista de todos os objetos desenháveis
            List<Drawable> allDrawables = graphNodes.Concat<Drawable>(graphLinks).ToList();

            // Se não houver objetos, retorne uma caixa padrão (ou lance uma exceção, dependendo do seu caso)
            if (!allDrawables.Any())
            {
                // Aqui você pode retornar uma caixa vazia, uma caixa padrão pequena ou null,
                // dependendo do que faz mais sentido para o seu aplicativo.
                // Por exemplo, uma caixa com ponto superior esquerdo e inferior direito iguais.
                return new Box(new Vector2D(0, 0), new Vector2D(0, 0));
            }

            // Inicialize canvasBox com a caixa do PRIMEIRO objeto
            Box canvasBox = allDrawables.First().GetBox(drawingDir);

            // Itere sobre os objetos restantes (a partir do segundo)
            foreach (Drawable drawable in allDrawables.Skip(1)) // Pula o primeiro elemento que já foi usado
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

