using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using AutoAVL.Settings;
using static System.TimeZoneInfo;

namespace AutoAVL.Drawables
{
    public class Link : Drawable
    {
        public Node start;
        public Node end;

        public string name;
        public Vector2D nameOffset;

        public double radiusPercentage;

        public Guid guid;

        public bool isAutoLink;
        public bool isInitialLink;


        /// <summary>
        /// Represents the auxiliary direction necessary to determine certain types of links.
        /// - In the case of an auto link, _directionAuxiliary represents the vector pointing to it's center.
        /// - In the case of an initial link, _directionAuxiliary represents the vector pointing where the link starts.
        /// - In the case of a standard link, _directionAuxiliary points to the middle point the links passes through.
        /// </summary>
        public Vector2D _directionAuxiliary;

        public Link(Node origin, Node destination, string name)
        {
            start = origin;
            end = destination;
            this.name = name;
            nameOffset = new Vector2D();
            guid = Guid.NewGuid();
            isAutoLink = (origin == destination);
            isInitialLink = false;
            _directionAuxiliary = origin.position.Middle(destination.position);
        }
        
        public Link(Node initialState)
        {
            start = initialState;
            end = initialState;
            this.name = "";
            nameOffset = new Vector2D();
            guid = Guid.NewGuid();
            isAutoLink = false;
            isInitialLink = true;
            _directionAuxiliary = new Vector2D();
        }

        public Link(Node initialState, Vector2D auxP, bool autoLink)
        {
            start = initialState;
            end = initialState;
            this.name = "";
            nameOffset = new Vector2D();
            guid = Guid.NewGuid();
            isAutoLink = autoLink;
            isInitialLink = autoLink ? false : true;
            _directionAuxiliary = auxP;
        }

        public static void PullLinks(List<Link> links, PhyD phyD)
        {
            foreach (Link link in links)
            {
                if (!link.isAutoLink)
                    PullLink(link, phyD);
            }
        }

        static void PullLink(Link link, PhyD phyD)
        {
            if (link.isAutoLink || link.isInitialLink)
                return;
                
            Vector2D nodeAPos = link.start.position;
            Vector2D nodeBPos = link.end.position;

            double distance = (nodeBPos - nodeAPos).Length();

            /* double restDistance = 100;
            double difDistance = distance - restDistance;
            if (difDistance > 0)
            {
                double force = (1 - phyD.attenuation - 0.4f) * phyD.elastic * difDistance;

                Vector2D forceDirection = (nodeBPos - nodeAPos).Normalized();

                link.start.displacement += force * forceDirection;
                link.end.displacement -= force * forceDirection;
            } */
/*             if (distance > 1000)
            {
                double force = (1 - phyD.attenuation - 0.4f) * phyD.elastic * distance;

                Vector2D forceDirection = (nodeBPos - nodeAPos).Normalized();

                link.start.displacement += force * forceDirection;
                link.end.displacement -= force * forceDirection;
            } */
            double force = (1 - phyD.attenuation) * phyD.elastic * distance * 2;

            Vector2D forceDirection = (nodeBPos - nodeAPos).Normalized();

            link.start.displacement += force * forceDirection;
            link.end.displacement -= force * forceDirection;
            
        }

        public static void SetUp(List<Link> links, DrawingDir drawingDir)
        {
            List<Guid> processedOpLinks = new List<Guid>();

            foreach (Link link in links.Where(link => !link.isAutoLink && !link.isInitialLink))
            {
                if (processedOpLinks.Contains(link.guid))
                    continue;

                Link opLink = links.Find(x => x.end == link.start && x.start == link.end);

                if (opLink != null)
                {
                    processedOpLinks.Add(opLink.guid);

                    double transitionDistance = (link.end.position - link.start.position).Length();
                    Vector2D middle = link.start.position.Middle(link.end.position);
                    Vector2D perpendicularVec = (link.end.position - link.start.position).Perpendicular();

                    link.radiusPercentage = drawingDir.linkRatio;
                    opLink.radiusPercentage = drawingDir.linkRatio;
                } 
                else
                {
                    Vector2D middle = link.start.position.Middle(link.end.position);

                    link._directionAuxiliary = middle;
                }
            }

            processedOpLinks.Clear();

            foreach (Link link in links.Where(link => link.isAutoLink))
            {
                List<Link> adjacentLinks = links.Where(adjacentLink => (adjacentLink.start == link.start || adjacentLink.end == link.end) && adjacentLink != link).ToList();

                Vector2D linkDirection = AcomodateAutoInitialLink(link.start.position, adjacentLinks);
                link._directionAuxiliary = linkDirection;
            }
            
            Link initialLink = links.Find(x => x.isInitialLink);
            List<Link> iAdjacentLinks = links.Where(adjacentLink => (adjacentLink.start == initialLink.start || adjacentLink.end == initialLink.end) && adjacentLink != initialLink).ToList();
            Vector2D initialLinkDirection = AcomodateAutoInitialLink(initialLink.start.position, iAdjacentLinks);
            initialLink._directionAuxiliary = initialLink.start.position + initialLinkDirection * (drawingDir.initialLinkSize + drawingDir.TotalRadius());
        }
        
        public static Vector2D AcomodateAutoInitialLink(Vector2D nodePos, List<Link> adjacentLinks)
        {
            List<Vector2D> adjacentDirections = new List<Vector2D>();

            foreach(Link adjacentLink in adjacentLinks)
            {
                if (adjacentLink.isInitialLink || adjacentLink.radiusPercentage == 0)
                    adjacentDirections.Add(adjacentLink._directionAuxiliary);
                else
                {
                    Vector2D positionStartNode = adjacentLink.start.position;
                    Vector2D positionEndNode = adjacentLink.end.position;
                    Vector2D positionTransitionMiddle = positionStartNode.Middle(positionEndNode);

                    Vector2D directionTransition = (positionEndNode - positionStartNode).Normalized();
                    Vector2D directionPerpendicular = directionTransition.Perpendicular();

                    double distanceTransition = (positionEndNode - positionStartNode).Length();
                    double ratioTransitionBumpToWidth = adjacentLink.radiusPercentage;

                    Vector2D positionArcSide = positionTransitionMiddle + directionPerpendicular * distanceTransition * ratioTransitionBumpToWidth;
                    Vector2D positionArcCenter = Vector2D.FindCenter(positionStartNode, positionArcSide, positionEndNode);

                    double radiusArc = (positionEndNode - positionArcCenter).Length();
                    double distanceTransitionCenterToMiddle = (positionArcCenter - positionTransitionMiddle).Length();

                    double distanceTransitionCenterToTangentialsIntersection = radiusArc * radiusArc / distanceTransitionCenterToMiddle;

                    Vector2D positionTangentialsIntersection = positionArcCenter + directionPerpendicular * distanceTransitionCenterToTangentialsIntersection;

                    Vector2D directionTangentialsIntersection = (positionTangentialsIntersection - nodePos).Normalized();

                    adjacentDirections.Add(directionTangentialsIntersection);
                }
            }

            adjacentDirections.Sort((v1, v2) => v1.UnsignedAngle().CompareTo(v2.UnsignedAngle()));
            
            Vector2D sectionStart = new Vector2D();
            Vector2D sectionEnd = new Vector2D();
            double sectionAngle = double.MinValue;
            
            for(int i = 0; i <= adjacentDirections.Count - 1; i++)
            {
                Vector2D currentVector = adjacentDirections[i];
                Vector2D nextVector = adjacentDirections[(i + 1) % adjacentDirections.Count];

                double angle = currentVector.UnsignedRotationAngle(nextVector);

                if (angle > sectionAngle)
                {
                    sectionAngle = angle;
                    sectionStart = adjacentDirections[i];
                    sectionEnd = adjacentDirections[(i + 1) % adjacentDirections.Count];
                }
            }
            
            return sectionStart.Rotated(sectionAngle / 2).Normalized();
        }

        public string ToSvg(DrawingDir drawingDir, SvgCanvas canvas)
        {
            if (isAutoLink)
            {
                return this.AutoLinkSVG(drawingDir, canvas);
            }
            else if (isInitialLink)
            {
                return this.GenerateStraightTransitionSVG(canvas, drawingDir);
            }
            else
            {
                if (radiusPercentage == 0)
                {
                    return this.GenerateStraightTransitionSVG(canvas, drawingDir);
                } 
                else
                {
                    return this.GenerateArcTransitionSVG(canvas, drawingDir); ;
                }
            }
        }

        public string ToLatex(DrawingDir drawingDir)
        {
            if (isAutoLink)
            {
                return this.AutoLinkLatex(drawingDir);
            }
            else if (isInitialLink)
            {
                return this.GenerateStraightTransitionLatex(drawingDir);
            }
            else
            {
                if (radiusPercentage == 0)
                {
                    return this.GenerateStraightTransitionLatex(drawingDir);
                } 
                else
                {
                    return this.GenerateArcTransitionLatex(drawingDir); ;
                }
            }
        }

        public Box GetBox(DrawingDir drawingDir)
        {
            if (isAutoLink)
            {
                double radius = drawingDir.AutoRadius() + 2 * drawingDir.textDistance;
                Vector2D topLeft = new Vector2D(_directionAuxiliary.x - radius, _directionAuxiliary.y + radius);
                Vector2D bottomRight = new Vector2D(_directionAuxiliary.x + radius, _directionAuxiliary.y - radius);

                return new Box(topLeft, bottomRight);
            }
            else if (isInitialLink)
            {
                return Box.EncompassingBox(_directionAuxiliary, start.position);
            }
            else
            {
                Vector2D perpendicular = (end.position - start.position).Perpendicular();
                Vector2D middle = start.position.Middle(end.position);

                double radiusTransition = (end.position - start.position).Length() / 2;
                Vector2D positionSide = middle + perpendicular * radiusPercentage * radiusTransition;

                return Box.EncompassingBox(start.position, positionSide, end.position);
            }
        }
        
        string GenerateArcTransitionSVG(SvgCanvas canvas, DrawingDir drawingDir)
        {
            string svgArc = "";

            Vector2D positionStartNode = canvas.ToSvgCoordinates(start.position);
            Vector2D positionEndNode = canvas.ToSvgCoordinates(end.position);

            Vector2D transitionDirection = (positionEndNode - positionStartNode).Normalized();
            Vector2D perpendicularDirection = transitionDirection.PerpendicularSVG();

            Vector2D middlePoint = positionStartNode.Middle(positionEndNode);

            double radiusTransition = (positionEndNode - positionStartNode).Length() / 2;

            Vector2D positionSide = middlePoint + perpendicularDirection * radiusPercentage * radiusTransition;
            
            Vector2D center = Vector2D.FindCenter(positionStartNode, positionEndNode, positionSide);

            double arcRadius = (positionSide - center).Length();

            // Find the arrow's tip and base points by rotating the vector from the arc's center 
            // by the angles formed by the node radius and the arrow length, respectively.
            // Note: Since the rotation is counterclockwise, we must use negative angles.
            double nodeRadiusAngle = Vector2D.AngleBetween(drawingDir.nodeRadius, arcRadius, arcRadius);
            double arrowBaseAngle = Vector2D.AngleBetween(drawingDir.arrowLength, arcRadius, arcRadius);

            Vector2D arrowTip = center + (positionEndNode - center).Rotated(-nodeRadiusAngle);
            Vector2D arrowBase = center + (positionEndNode - center).Rotated(-(nodeRadiusAngle + arrowBaseAngle));
            
            Vector2D arcOrigin = center + (positionStartNode - center).Rotated(nodeRadiusAngle);
            Vector2D arcDestination = arrowBase;
            
            Vector2D textPosition = positionSide + perpendicularDirection * 20;

            Vector2D directionMiddleToCenter = (center - middlePoint).Normalized();
            Vector2D directionMiddleToSide = (positionSide - middlePoint).Normalized();
            
            // Between two points exist four possible arcs in SVG.
            // largeArcFlag determinnes if the arc will be the smallest or biggest between the points.
            // sweepFlag determines if the arc will have a clockwise or counter-clockwise rotation.
            int largeArcFlag = directionMiddleToCenter == directionMiddleToSide ? 1 : 0;
            int sweepFlag = radiusPercentage >= 0 ? 0 : 1;
            
            // All points need to be converted to the SVG canva's coordinate system.
            /* arcOrigin = canvas.ToSvgCoordinates(arcOrigin);
            arcDestination = canvas.ToSvgCoordinates(arcDestination);
            arrowBase = arcDestination;
            arrowTip = canvas.ToSvgCoordinates(arrowTip); */
            
            // Construct the arc's path attribute.
            string arcPath = $"M {arcOrigin.x} {arcOrigin.y} A {arcRadius} {arcRadius} 0 {largeArcFlag} {sweepFlag} {arcDestination.x} {arcDestination.y}";

            // Line attributes.
            string stroke = $"stroke=\"{drawingDir.strokeColor}\"";
            string strokeWidth = $"stroke-width=\"{drawingDir.linkStrokeWidth}\"";
            string fill = "fill=\"none\"";

            svgArc += $"<path d=\"{arcPath}\" {stroke} {strokeWidth} {fill} />{Environment.NewLine}";
            svgArc += GenerateArrowheadPolygonSVG(arrowTip, arrowBase, drawingDir);
            svgArc += GenerateSvgTextElement(textPosition, perpendicularDirection, canvas, name, drawingDir);
            
            
            return svgArc;
        }

        string GenerateArcTransitionLatex(DrawingDir drawingDir)
        {
            string latexArc = "";
            Console.WriteLine("Desenhando a transição curva em Latex");

            Console.WriteLine("Posição do node inicial = " + start.position);
            Console.WriteLine("Posição do node final = " + end.position);
            
            Vector2D transitionDirection = (end.position - start.position).Normalized();
            Vector2D perpendicularDirection = transitionDirection.Perpendicular();
            Vector2D middlePoint = start.position.Middle(end.position);
            Console.WriteLine("Direção da transição = " + transitionDirection);
            Console.WriteLine("Direção perpendicular da transição = " + perpendicularDirection);
            Console.WriteLine("Posição do meio da transição = " + middlePoint);

            double distanceTransition = (end.position - start.position).Length();
            Console.WriteLine("Distância da transição = " + distanceTransition);

            Vector2D positionSide = middlePoint + perpendicularDirection * radiusPercentage * distanceTransition / 2;
            Console.WriteLine("Posição do ponto lateral = " + positionSide);
            
            Vector2D center = Vector2D.FindCenter(start.position, end.position, positionSide);
            Console.WriteLine("Posição do centro = " + center);

            double arcRadius = (positionSide - center).Length();
            Console.WriteLine("Tamanho do raio do arco = " + arcRadius);

            // Find the arrow's tip and base points by rotating the vector from the arc's center 
            // by the angles formed by the node radius and the arrow length, respectively.
            // Note: Since the rotation is counterclockwise, we must use negative angles.
            double nodeRadiusAngle = Vector2D.AngleBetween(drawingDir.nodeRadius, arcRadius, arcRadius);
            double arrowBaseAngle = Vector2D.AngleBetween(drawingDir.arrowLength, arcRadius, arcRadius);
            Console.WriteLine("Ângulo do raio do node = " + nodeRadiusAngle);
            Console.WriteLine("Ângulo da base da seta = " + arrowBaseAngle);

            int factorAngleCorretion = radiusPercentage > 0 ? 1 : -1;

            Vector2D arrowTip = center + (end.position - center).Rotated(-nodeRadiusAngle * factorAngleCorretion);
            Vector2D arrowBase = center + (end.position - center).Rotated(-(nodeRadiusAngle + arrowBaseAngle) * factorAngleCorretion);
            Console.WriteLine("Posição da ponta da seta = " + arrowTip);
            Console.WriteLine("Posição da base da seta = " + arrowBase);

            Vector2D tempDirection = (start.position - center);
            Console.WriteLine("Direção do centro da transição para o node inicial = " + tempDirection);
            Console.WriteLine("Direção rotacionada para o início da transição = " + tempDirection.Rotated(nodeRadiusAngle * factorAngleCorretion));
            Vector2D arcOrigin = center + (start.position - center).Rotated(nodeRadiusAngle * factorAngleCorretion);
            Vector2D arcDestination = arrowBase;
            Console.WriteLine("Posição da origem do arco = " + arcOrigin);
            Console.WriteLine("Posição do destino do arco = " + arcDestination);
            
            Vector2D textPosition = positionSide + perpendicularDirection * drawingDir.textDistance;
            Console.WriteLine("Posição do texto = " + textPosition);

            Vector2D directionMiddleToCenter = (center - middlePoint).Normalized();
            Vector2D directionMiddleToSide = (positionSide - middlePoint).Normalized();
            
            // Between two points exist four possible arcs in SVG.
            // largeArcFlag determinnes if the arc will be the smallest or biggest between the points.
            // sweepFlag determines if the arc will have a clockwise or counter-clockwise rotation.
            int largeArcFlag = directionMiddleToCenter == directionMiddleToSide ? 1 : 0;
            int sweepFlag = radiusPercentage >= 0 ? 0 : 1;

            Vector2D directionCenterToStart = (arcOrigin - center).Normalized();
            Vector2D directionCenterToEnd = (arcDestination - center).Normalized();
            Console.WriteLine("Direção do centro ao início = " + directionCenterToStart);
            Console.WriteLine("Direção do centro ao fim = " + directionCenterToEnd);

            double angleAutoLinkStart = (arcOrigin - center).UnsignedAngleDegrees();
            double angleAutoLinkEnd = (arcDestination - center).UnsignedAngleDegrees();
            Console.WriteLine("Ângulo de início = " + angleAutoLinkStart);
            Console.WriteLine("Ângulo de fim = " + angleAutoLinkEnd);

            double angleSmaller = Vector2D.SmallestAngleBetweenVectorsDegrees(directionCenterToStart, directionCenterToEnd);
            double angleBigger = 360 - angleSmaller;
            Console.WriteLine("Menor ângulo = " + angleSmaller);
            Console.WriteLine("Maio ângulo = " + angleBigger);

            double angleToInclude = largeArcFlag == 1 ? angleBigger : angleSmaller;
            int includeOperation = sweepFlag == 1 ? -1 : 1;

            angleAutoLinkEnd = angleAutoLinkStart + includeOperation * angleToInclude;
            
            latexArc += @"\draw [black] (" + arcOrigin.x / 10 + "," + arcOrigin.y / 10 + ") arc (" + angleAutoLinkStart + ":" + angleAutoLinkEnd + ":" + arcRadius / 10 + ");" + Environment.NewLine;

            Console.WriteLine("Linha final do arco = " + latexArc);

            latexArc += GenerateArrowheadPolygonLatex(arrowTip, arrowBase, drawingDir);
            latexArc += GenerateLatexTextElement(textPosition, perpendicularDirection, name, drawingDir);
            
            
            return latexArc;
        }
        
        string GenerateStraightTransitionSVG(SvgCanvas canvas, DrawingDir drawingDir)
        {
            Vector2D positionStartNode = canvas.ToSvgCoordinates(start.position);
            Vector2D positionEndNode = canvas.ToSvgCoordinates(end.position);

            Vector2D positionAuxiliary = canvas.ToSvgCoordinates(_directionAuxiliary);

            Vector2D directionTransition = (positionEndNode - positionStartNode).Normalized();
            Vector2D directionPerpendicular = directionTransition.PerpendicularSVG();

            if (isInitialLink) directionTransition = (positionEndNode - positionAuxiliary).Normalized();
            
            Vector2D arrowTip = positionEndNode - directionTransition * drawingDir.nodeRadius;
            Vector2D arrowBase = positionEndNode - directionTransition * (drawingDir.nodeRadius + drawingDir.arrowLength);

            Vector2D lineOrigin = positionStartNode + directionTransition * drawingDir.nodeRadius;
            if (isInitialLink) lineOrigin = positionAuxiliary;
            
            Vector2D lineDestination = arrowBase;

            Vector2D textPosition = positionAuxiliary + directionPerpendicular * drawingDir.textDistance;
            
            // Convert the points to the SVG canva's coordinate system.
            /* arrowTip = canvas.ToSvgCoordinates(arrowTip);
            arrowBase = canvas.ToSvgCoordinates(arrowBase);
            lineOrigin = canvas.ToSvgCoordinates(lineOrigin);
            lineDestination = canvas.ToSvgCoordinates(lineDestination); */
            
            string svgLine = $"<line x1=\"{lineOrigin.x}\" y1=\"{lineOrigin.y}\" x2=\"{lineDestination.x}\" y2=\"{lineDestination.y}\" stroke=\"{drawingDir.strokeColor}\" stroke-width=\"{drawingDir.linkStrokeWidth}\" />{Environment.NewLine}";
            
            svgLine += GenerateArrowheadPolygonSVG(arrowTip, arrowBase, drawingDir);
            if (isInitialLink) return svgLine;
            
            svgLine += GenerateSvgTextElement(textPosition, directionPerpendicular, canvas, name, drawingDir);

            return svgLine;
        }

        string GenerateStraightTransitionLatex(DrawingDir drawingDir)
        {
            Vector2D transitionDirection = (end.position - start.position).Normalized();
            if (isInitialLink) transitionDirection = (end.position - _directionAuxiliary).Normalized();
            
            Vector2D perpendicularDirection = transitionDirection.Perpendicular();
            
            Vector2D arrowTip = end.position - transitionDirection * drawingDir.nodeRadius;
            Vector2D arrowBase = end.position - transitionDirection * (drawingDir.nodeRadius + drawingDir.arrowLength);

            Vector2D lineOrigin = start.position + transitionDirection * drawingDir.nodeRadius;
            if (isInitialLink) lineOrigin = _directionAuxiliary;
            
            Vector2D lineDestination = arrowBase;

            Vector2D textPosition = _directionAuxiliary + perpendicularDirection * drawingDir.textDistance;

            string latexLineElement = @"\draw [black] (" + lineOrigin.x / 10 + "," + lineOrigin.y / 10 + ") -- (" + lineDestination.x / 10 + "," + lineDestination.y / 10 + ");" + Environment.NewLine; 

            latexLineElement += GenerateArrowheadPolygonLatex(arrowTip, arrowBase, drawingDir);
            
            latexLineElement += GenerateLatexTextElement(textPosition, perpendicularDirection, name, drawingDir);

            return latexLineElement;
        }

        /* string GenerateCurvedTransitionSVG(SvgCanvas canvas, DrawingDir drawingDir)
        {
            Vector2D transitionDirection = (end.position - start.position).Normalized();
            Vector2D perpendicularDirection = transitionDirection.Perpendicular();

            Vector2D transitionOrigin = start.position + ((_directionAuxiliary - start.position).Normalized() * drawingDir.nodeRadius);
            Vector2D transitionDestination = end.position + ((_directionAuxiliary - end.position).Normalized() * drawingDir.nodeRadius);

            Vector2D controlPointSVG = canvas.ToSvgCoordinates(_directionAuxiliary);
            Vector2D originSVG = canvas.ToSvgCoordinates(transitionOrigin);
            Vector2D destinationSVG = canvas.ToSvgCoordinates(transitionDestination);

            Vector2D textPosition = _directionAuxiliary;// + perpendicularDirection * drawingDir.textDistance;

            string svgArrowElement = GenerateCurvedArrowSVG(originSVG, destinationSVG, controlPointSVG, drawingDir);
            string svgTextElement = GenerateSvgTextElement(textPosition, perpendicularDirection, canvas, name, drawingDir);

            string svgRepresentation = svgArrowElement + svgTextElement;

            return svgRepresentation;
        } */

        /* static string GenerateCurvedArrowSVG(Vector2D origin, Vector2D destination, Vector2D control, DrawingDir drawingDir)
        {
            Vector2D center = Vector2D.FindCenter(origin, destination, control);
            double arcRadius = (control - center).Length();

            // Find the arrow's tip and base points by rotating the vector from the arc's center 
            // by the angles formed by the node radius and the arrow length, respectively.
            // Note: Since the rotation is counterclockwise, we must use negative angles.
            double arrowPointAngle = Vector2D.AngleBetween(drawingDir.nodeRadius, arcRadius, arcRadius);
            double arrowBaseAngle = Vector2D.AngleBetween(drawingDir.arrowLength, arcRadius, arcRadius);
            Vector2D arrowTip = center + (destination - center).Rotated(-arrowPointAngle);
            Vector2D arrowBase = center + (destination - center).Rotated(-(arrowPointAngle + arrowBaseAngle));


            Vector2D insideArrow = destination + (control - destination).Normalized() * drawingDir.arrowLength / 2;

            string svgPathElement = GenerateQuadraticBezierPathSVG(origin, insideArrow, control, drawingDir);
            string svgArrowheadElement = GenerateArrowheadPolygonSVG(destination, drawingDir, destination - control);

            string svgRepresentation = svgPathElement + svgArrowheadElement;

            return svgRepresentation;
        } */


        /* static string GenerateArcPathSVG(Vector2D origin, Vector2D destination, Vector2D control, DrawingDir drawingDir)
        {
            // Construct the path attribute.
            string path = $"M {origin.x} {origin.y} Q {control.x} {control.y} {destination.x} {destination.y}";

            // Construct the stroke attribute.
            string stroke = $"stroke=\"{drawingDir.strokeColor}\"";

            // Construct the stroke-width attribute.
            string strokeWidth = $"stroke-width=\"{drawingDir.linkStrokeWidth}\"";

            // Construct the fill attribute.
            string fill = "fill=\"none\"";

            // Construct the SVG path element.
            string svgPathElement = $"<path d=\"{path}\" {stroke} {strokeWidth} {fill} />{Environment.NewLine}";

            return svgPathElement;
        } */

        /* static string GenerateQuadraticBezierPathSVG(Vector2D origin, Vector2D destination, Vector2D control, DrawingDir drawingDir)
        {
            // Construct the path attribute.
            string path = $"M {origin.x} {origin.y} Q {control.x} {control.y} {destination.x} {destination.y}";

            // Construct the stroke attribute.
            string stroke = $"stroke=\"{drawingDir.strokeColor}\"";

            // Construct the stroke-width attribute.
            string strokeWidth = $"stroke-width=\"{drawingDir.linkStrokeWidth}\"";

            // Construct the fill attribute.
            string fill = "fill=\"none\"";

            // Construct the SVG path element.
            string svgPathElement = $"<path d=\"{path}\" {stroke} {strokeWidth} {fill} />{Environment.NewLine}";

            return svgPathElement;
        } */

        /* static string GenerateStraightArrowSVG(Vector2D origin, Vector2D destination, DrawingDir drawingDir)
        {
            Vector2D insideArrow = destination + (origin - destination).Normalized() * drawingDir.arrowLength / 2;

            string svgLineElement = GenerateLineElementSVG(origin, insideArrow, drawingDir);
            string svgArrowheadElement = GenerateArrowheadPolygonSVG(destination, drawingDir, destination - origin);

            string svgRepresentation = svgLineElement + svgArrowheadElement;

            return svgRepresentation;
        } */

        /* static string GenerateLineElementSVG(Vector2D origin, Vector2D destination, DrawingDir drawingDir)
        {
            string svgLineElement = $"<line x1=\"{origin.x}\" y1=\"{origin.y}\" x2=\"{destination.x}\" y2=\"{destination.y}\" stroke=\"{drawingDir.strokeColor}\" stroke-width=\"{drawingDir.linkStrokeWidth}\" />{Environment.NewLine}";

            return svgLineElement;
        } */

        string AutoLinkSVG(DrawingDir drawingDir, SvgCanvas canvas)
        {
            string autoLinkSVG = "";
            
            Vector2D positionNode = canvas.ToSvgCoordinates(start.position);
            Vector2D directionAutoLinkCenter = _directionAuxiliary.ToSvgDirection();

            double autoRadius = drawingDir.AutoRadius();
            double distanceNodeToAutoLinkCenter = drawingDir.autoLinkDistanceRatio * autoRadius + drawingDir.TotalRadius();
            
            Vector2D positionAutoLinkCenter =  positionNode + directionAutoLinkCenter * (drawingDir.autoLinkDistanceRatio * autoRadius + drawingDir.TotalRadius());
            /* double transitionDistance = (start.position - _directionAuxiliary).Length(); */

            double nodeAngle = Vector2D.AngleBetween(drawingDir.TotalRadius(), distanceNodeToAutoLinkCenter, drawingDir.AutoRadius());
            double arrowAngle = Vector2D.AngleBetween(drawingDir.arrowLength, autoRadius, autoRadius);

            // Since the auto link has a bit of interposition with the node it is connected to, positioning the arrow must take this into account.
            Vector2D positionArrowTip = positionAutoLinkCenter - directionAutoLinkCenter.Rotated(nodeAngle) * autoRadius;
            Vector2D positionArrowBase = positionAutoLinkCenter - directionAutoLinkCenter.Rotated(nodeAngle + arrowAngle) * autoRadius;

            Vector2D directionArrow = (positionArrowTip - positionArrowBase).Normalized();
            Vector2D directionArrowPerpendicular = directionArrow.Perpendicular();

            Vector2D originPoint = positionAutoLinkCenter - directionAutoLinkCenter.Rotated(-nodeAngle) * drawingDir.autoLinkRadius; 
            Vector2D endPoint = positionArrowBase;
            
            /* // The start of the arc is in the opposite of the arrow, so the angle is negative compared to the angle used previously.
            _positionArcStart = positionAutoLinkCenter - directionAutoLinkCenter.Rotated(-_angleAutoLinkStart) * _radiusAutoLink; 
            _positionArcEnd = positionArrowBase;

            _positionArrowTip = positionArrowTip;
            _positionRightWing = _positionArrowTip - directionArrow * _arrowLength + directionArrowPerpendicular * _arrowWidth / 2;
            _positionLeftWing = _positionArrowTip - directionArrow * _arrowLength - directionArrowPerpendicular * _arrowWidth / 2;

            _positionText = positionAutoLinkCenter + directionAutoLinkCenter * (_radiusAutoLink + 20);
            
            
            double nodeAngle = Vector2D.AngleBetween(drawingDir.TotalRadius(), distanceAutoToCenter, drawingDir.AutoRadius());
            double arrowAngle = Vector2D.AngleBetween(drawingDir.arrowLength, autoRadius, autoRadius);
            
            Vector2D arrowTip = _directionAuxiliary + transitionDirection.Rotated(nodeAngle) * drawingDir.autoLinkRadius;
            Vector2D arrowBase = _directionAuxiliary + transitionDirection.Rotated(nodeAngle + arrowAngle) * drawingDir.autoLinkRadius;  */
            
            /* Vector2D originPoint = _directionAuxiliary + transitionDirection.Rotated(-nodeAngle) * drawingDir.autoLinkRadius; 
            Vector2D endPoint = arrowBase; */
            
            Vector2D textPosition = positionAutoLinkCenter + directionAutoLinkCenter * (autoRadius + 20);
            
            /* // All points need to be converted to the SVG canva's coordinate system.
            originPoint = canvas.ToSvgCoordinates(originPoint);
            endPoint = canvas.ToSvgCoordinates(endPoint);
            arrowBase = endPoint;
            arrowTip = canvas.ToSvgCoordinates(arrowTip); */
            
            // Construct the arc's path attribute.
            string path = $"M {originPoint.x} {originPoint.y} A {autoRadius} {autoRadius} 0 1 0 {endPoint.x} {endPoint.y}";

            // Line attributes.
            string stroke = $"stroke=\"{drawingDir.strokeColor}\"";
            string strokeWidth = $"stroke-width=\"{drawingDir.linkStrokeWidth}\"";
            string fill = "fill=\"none\"";

            autoLinkSVG += $"<path d=\"{path}\" {stroke} {strokeWidth} {fill} />{Environment.NewLine}";
            autoLinkSVG += GenerateArrowheadPolygonSVG(positionArrowTip, positionArrowBase, drawingDir);
            autoLinkSVG += GenerateSvgTextElement(textPosition, directionAutoLinkCenter, canvas, name, drawingDir);
            
            return autoLinkSVG;
        }

        string AutoLinkLatex(DrawingDir drawingDir)
        {
            string autoLinkSVG = "";

            Vector2D positionAutoLinkCenter = start.position + _directionAuxiliary * (drawingDir.TotalRadius() + drawingDir.AutoRadius() * drawingDir.autoLinkDistanceRatio);
            
            Vector2D transitionDirection = _directionAuxiliary;
            double transitionDistance = (start.position - positionAutoLinkCenter).Length();
            double autoRadius = drawingDir.AutoRadius();
            
            double nodeAngle = Vector2D.AngleBetween(drawingDir.TotalRadius(), transitionDistance, drawingDir.AutoRadius());
            double arrowAngle = Vector2D.AngleBetween(drawingDir.arrowLength, autoRadius, autoRadius);
            
            Vector2D arrowTip = positionAutoLinkCenter - transitionDirection.Rotated(-nodeAngle) * drawingDir.autoLinkRadius;
            Vector2D arrowBase = positionAutoLinkCenter - transitionDirection.Rotated(-(nodeAngle + arrowAngle)) * drawingDir.autoLinkRadius; 
            
            Vector2D originPoint = positionAutoLinkCenter - transitionDirection.Rotated(nodeAngle) * drawingDir.autoLinkRadius; 
            Vector2D endPoint = arrowBase;
            
            Vector2D textPosition = positionAutoLinkCenter;

            /* double angleAutoLinkStart = (originPoint - positionAutoLinkCenter).UnsignedAngle();
            double angleAutoLinkEnd = (endPoint - positionAutoLinkCenter).UnsignedAngle(); */

            int largeArcFlag = 1;
            int sweepFlag = 0;

            Vector2D directionCenterToStart = (originPoint - positionAutoLinkCenter).Normalized();
            Vector2D directionCenterToEnd = (endPoint - positionAutoLinkCenter).Normalized();
            Console.WriteLine("Direção do centro ao início = " + directionCenterToStart);
            Console.WriteLine("Direção do centro ao fim = " + directionCenterToEnd);

            double angleAutoLinkStart = (originPoint - positionAutoLinkCenter).UnsignedAngleDegrees();
            double angleAutoLinkEnd = (endPoint - positionAutoLinkCenter).UnsignedAngleDegrees();
            Console.WriteLine("Ângulo de início = " + angleAutoLinkStart);
            Console.WriteLine("Ângulo de fim = " + angleAutoLinkEnd);

            double angleSmaller = Vector2D.SmallestAngleBetweenVectorsDegrees(directionCenterToStart, directionCenterToEnd);
            double angleBigger = 360 - angleSmaller;
            Console.WriteLine("Menor ângulo = " + angleSmaller);
            Console.WriteLine("Maio ângulo = " + angleBigger);

            double angleToInclude = largeArcFlag == 1 ? angleBigger : angleSmaller;
            int includeOperation = sweepFlag == 1 ? -1 : 1;

            angleAutoLinkEnd = angleAutoLinkStart + includeOperation * angleToInclude;
            
            autoLinkSVG += @"\draw [black] (" + originPoint.x / 10 + "," + originPoint.y / 10 + ") arc (" + angleAutoLinkStart + ":" + angleAutoLinkEnd + ":" + autoRadius / 10 + ");" + Environment.NewLine;
            autoLinkSVG += GenerateArrowheadPolygonLatex(arrowTip, arrowBase, drawingDir);
            autoLinkSVG += GenerateLatexTextElement(textPosition, transitionDirection, name, drawingDir);
            
            return autoLinkSVG;
        }

        static string GenerateArrowheadPolygonSVG(Vector2D arrowTip, Vector2D arrowBase, DrawingDir drawingDir)
        {
            Vector2D arrowDirection = (arrowTip  - arrowBase).Normalized();
            Vector2D perpendicular = arrowDirection.Perpendicular();
            // Calculate the side points of the arrowhead by adding/subtracting the perpendicular direction vector multiplied by half of the arrow width to/from the base point.
            Vector2D sidePoint1 = arrowBase + perpendicular * drawingDir.arrowWidth / 2;
            Vector2D sidePoint2 = arrowBase - perpendicular * drawingDir.arrowWidth / 2;

            // Construct the SVG polygon element with the calculated points, arrow color, and stroke width.
            string svgPolygonElement = $"<polygon fill=\"{drawingDir.arrowColor}\" stroke-width=\"1\" " +
                $"points=\"{arrowTip.x} {arrowTip.y} {sidePoint1.x} {sidePoint1.y} {sidePoint2.x} {sidePoint2.y}\" />{Environment.NewLine}";

            return svgPolygonElement;
        }

        static string GenerateArrowheadPolygonLatex(Vector2D arrowTip, Vector2D arrowBase, DrawingDir drawingDir)
        {
            Vector2D arrowDirection = (arrowTip  - arrowBase).Normalized();
            Vector2D perpendicular = arrowDirection.Perpendicular();
            // Calculate the side points of the arrowhead by adding/subtracting the perpendicular direction vector multiplied by half of the arrow width to/from the base point.
            Vector2D sidePoint1 = arrowBase + perpendicular * drawingDir.arrowWidth / 2;
            Vector2D sidePoint2 = arrowBase - perpendicular * drawingDir.arrowWidth / 2;

            string latexPolygonElement = @"\fill [black] (" + arrowTip.x / 10 + "," + arrowTip.y / 10 + ") -- (" + sidePoint1.x / 10 + "," + sidePoint1.y / 10 + ") -- (" + sidePoint2.x / 10 + "," + sidePoint2.y / 10 + ");" + Environment.NewLine; 

            return latexPolygonElement;
        }

        static string GenerateSvgTextElement(Vector2D position, Vector2D direction, SvgCanvas canvas, string text, DrawingDir drawingDir)
        {
            // Calculate the angle based on the direction.
            double angle = direction.UnsignedAngle();
            
            double anchorAngleDeg = 60.0f;
            double anchorAngleRad = anchorAngleDeg *  Math.PI / 180;
            
            // Determine the text anchor based on the angle.
            string textAnchor;
            if (angle >= 2 *  Math.PI - anchorAngleRad || angle <= anchorAngleRad)
                textAnchor = "start";
            else if (angle >=  Math.PI - anchorAngleRad && angle <=  Math.PI + anchorAngleRad)
                textAnchor = "end";
            else
                textAnchor = "middle";

            textAnchor = "middle";

                
            string dominantBaseline;
            if (angle >  Math.PI)
                dominantBaseline = "hanging";
            else
                dominantBaseline = "auto";
                
                

            // Convert the position to SVG coordinates.
            Vector2D svgPosition = position;

            // Construct the SVG text element with the specified attributes.
            string svgTextElement = $@"
                <text x=""{svgPosition.x}"" y=""{svgPosition.y}"" 
                      text-anchor=""{textAnchor}"" 
                      dominant-baseline=""{dominantBaseline}"" 
                      font-size=""{drawingDir.textSize}"" fill=""{drawingDir.textColor}"">
                    {text}
                </text>
                {Environment.NewLine}";

            return svgTextElement;
        }

        static string GenerateLatexTextElement(Vector2D position, Vector2D direction, string text, DrawingDir drawingDir)
        {
            if (text == "")
                return "";
                
            // Calculate the angle based on the direction.
            double angle = direction.UnsignedAngle();
            
            double anchorAngleDeg = 60.0f;
            double anchorAngleRad = anchorAngleDeg *  Math.PI / 180;
            
            // Determine the text anchor based on the angle.
            string textAnchor;
            if (angle >= 2 *  Math.PI - anchorAngleRad || angle <= anchorAngleRad)
                textAnchor = "start";
            else if (angle >=  Math.PI - anchorAngleRad && angle <=  Math.PI + anchorAngleRad)
                textAnchor = "end";
            else
                textAnchor = "middle";

            textAnchor = "middle";

                
            string dominantBaseline;
            if (angle >  Math.PI)
                dominantBaseline = "hanging";
            else
                dominantBaseline = "auto";

            string latexTextElement = @"\draw (" + position.x / 10 + "," + position.y / 10 + ") node [above] {$" + text + "$};" + Environment.NewLine;

            return latexTextElement;
        }
    }
}
