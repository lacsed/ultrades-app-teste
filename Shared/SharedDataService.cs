using AutoAVL;
using AutoAVL.Drawables;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using UltraDESDraw.Components;

namespace UltraDESDraw.Services 
{
    public class SharedDataService
    {
        public event Action OnChange;
        public string SomeData {get; set; } = "Default";
        public Graph Graph {get; set; } = new Graph();
        public Node? SelectedNode {get; set; }
        public Node? InsideNode {get; set; }
        public EditLink? SelectedLink {get; set; }

        public Vector2D? TempLinkStart {get; set; }
        public Vector2D? TempLinkEnd {get; set; }
        public Node? StartNode {get; set; }
        public Node? EndNode {get; set; }
        public Vector2D? _directionAuxiliary {get; set; }

        #region Variáveis de estado da canvas.
        private string cursor = "cursor: default;";
        private bool _panning = false;
        private bool _movingNode = false;
        private bool _movingLink = false;
        private bool _creatingLink = false;
        private bool _initialTransition = false;
        private bool _holdingSpace = false;
        private bool _holdingShift = false;
        private bool _mouseDown = false;
        private Vector2D _mousePosition = new Vector2D();
        #endregion

        public Dictionary<string, Graph> automata = new Dictionary<string, Graph>();

        public void NotifyDataChanged() => OnChange?.Invoke();

        // Access functions to private variables.

        public bool GetIsCreatingLink()
        {
            return _creatingLink;
        }

        public string GetCursorType()
        {
            return cursor;
        }

        public bool IsCreatingInitialTransition()
        {
            return _initialTransition;
        }

        private void ClearActions()
        {
            _panning = false;
            _movingNode = false;
            _movingLink = false;
            _creatingLink = false;
            _initialTransition = false;
        }

        private void ClearLinkData()
        {
            StartNode = null;
            EndNode = null;
            TempLinkStart = null;
            TempLinkEnd = null;
        }

        public void UpdateAutomaton()
        {
            Graph.UpdateAutomaton();
        }

        public void NodeEnter(Node node)
        {
            InsideNode = node;

            if (node == StartNode)
                return;

            if (_initialTransition)
            {
                EndNode = node;
            }
            else if (StartNode != null)
            {
                EndNode = node;
            }

            NotifyDataChanged();
        }
        public void NodeLeave(Node node)
        {
            if (EndNode == node)
                EndNode = null;

            InsideNode = null;

            NotifyDataChanged();
        }
        public void CanvasMouseDownEvent(MouseEventArgs e)
        {
            if (e.Button == 0)
            {
                _mouseDown = true;

                if (_holdingSpace)
                {
                    _panning = true;
                }
                else if (e.ShiftKey)
                {
                    _creatingLink = true;

                    if (InsideNode == null)
                    {
                        Console.WriteLine("Agora é transição inicial");
                        _initialTransition = true;
                        TempLinkStart = _mousePosition;
                        Console.WriteLine("Posição do mouse " + _mousePosition);
                    }
                    else
                        StartNode = InsideNode;

                    TempLinkEnd = _mousePosition;
                    
                }
                else if (InsideNode != null)
                {
                    _movingNode = true;
                    cursor = "cursor: move;";
                }
                else if (SelectedLink != null)
                {
                    _movingLink = true;
                    cursor = "cursor: move;";
                }
            }

            NotifyDataChanged();
        }

        public void CanvasMouseUpEvent(MouseEventArgs e)
        {
            if (e.Button == 0)
            {
                _mouseDown = false;

                if (_panning)
                {
                    _panning = false;
                    cursor = "cursor: default;";
                } 
                else if (_creatingLink)
                {
                    if (EndNode != null)
                    {
                        if (_initialTransition)
                        {
                            Console.WriteLine("Criando link inicial");
                            Vector2D positionInitialLinkStart = TempLinkStart.FromSvgCoordinates(this.Graph.svgCanvas.SVGOrigin());
                            Console.WriteLine("posição inicial = " + positionInitialLinkStart);
                            Vector2D positionNode = EndNode.position;

                            Vector2D directionAuxiliary = (positionInitialLinkStart - positionNode).Normalized();
                            double size = (positionInitialLinkStart - positionNode).Length() - Graph.drawingDir.TotalRadius() - Graph.drawingDir.arrowLength;
                            var newLink = new Link(EndNode, directionAuxiliary, false);
                            newLink.radiusPercentage = size;
                            Graph.AddInitialLink(newLink);
                        }
                        else
                        {
                            Console.WriteLine("Antes dois");
                            var newLink = new Link(StartNode, EndNode, "");
                            Console.WriteLine("Depois dois");
                            newLink.radiusPercentage = 0;
                            Console.WriteLine(newLink == null);
                            Console.WriteLine(newLink.start == null);
                            Console.WriteLine(newLink.end == null);
                            Graph.AddLink(newLink);
                            Console.WriteLine("Depois 3");
                        }
                    }
                    else if (InsideNode != null)
                    {
                        _directionAuxiliary = (_mousePosition.FromSvgCoordinates(this.Graph.svgCanvas.SVGOrigin()) - StartNode.position).Normalized();
                        var newLink = new Link(InsideNode, _directionAuxiliary, true);

                        Graph.AddAutoLink(newLink);
                    }
                    
                } else if (_movingNode || _movingLink)
                {
                    cursor = "cursor: default;";
                }

                ClearActions();
                ClearLinkData();
                NotifyDataChanged();
            }
        }

        public void CanvasKeyDownEvent(KeyboardEventArgs e)
        {
            if (e.Key == " " && !_panning)
            {
                _holdingSpace = true;
                cursor = "cursor: grab;";
                NotifyDataChanged();
            }
            else if (e.Key == "Shift")
            {
                _holdingShift = true;
            }
        }

        public void CanvasKeyUpEvent(KeyboardEventArgs e)
        {
            if (e.Key == " ")
            {
                if (!(_panning || _movingNode || _movingLink || _creatingLink))
                {
                    Box encompassingBox = Graph.GetCanvasLimits();
                    Vector2D newSVGOrigin = encompassingBox.GetTopLeft();
                    Graph.svgCanvas.ChangeOrigin(newSVGOrigin);
                    cursor = "cursor: default;";
                }
                _holdingSpace = false;
                NotifyDataChanged();
            }
            else if (e.Key == "Shift")
            {
                _holdingShift = false;
            }
        }

        public void CanvasMouseMoveEvent(MouseEventArgs e, double _offsetX, double _offsetY, double zoomScale)
        {
            double worldMouseX = -_offsetX + e.OffsetX / zoomScale;
            double worldMouseY = -_offsetY + e.OffsetY / zoomScale;

            _mousePosition = new Vector2D(worldMouseX, worldMouseY);

            double MovementX = e.MovementX / zoomScale;
            double MovementY = e.MovementY / zoomScale;

            if (_panning)
            {
                Graph.svgCanvas.MoveOrigin(new Vector2D(-MovementX, MovementY));
                NotifyDataChanged();
            }
            else if (_movingNode)
            {
                SelectedNode.position += new Vector2D(MovementX, -MovementY);
                NotifyDataChanged();
            }
            else if (_movingLink)
            {
                SelectedLink.HandleMouseMove(_mousePosition);
            }
            else if (_creatingLink)
            {
                TempLinkEnd = _mousePosition;
                NotifyDataChanged();
            }
        }
    }
}