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
        public bool isControllable = false;


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
            _directionAuxiliary = (destination.position - origin.position).Normalized();
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

        public Link(Node initialState, Vector2D directionAuxiliary, bool autoLink)
        {
            start = initialState;
            end = initialState;
            this.name = "";
            nameOffset = new Vector2D();
            guid = Guid.NewGuid();
            isAutoLink = autoLink;
            isInitialLink = !autoLink;
            _directionAuxiliary = directionAuxiliary;
        }

        public override bool Equals(object obj)
        {
            // 1. Verificar se o objeto é nulo
            if (obj == null) return false;

            // 2. Verificar se o objeto é do mesmo tipo
            // (ou se pode ser convertido para Link)
            if (obj is Link otherLink)
            {
                // 3. Comparar pela propriedade que define a identidade única
                // Neste caso, o GUID é perfeito.
                return this.guid.Equals(otherLink.guid);
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Retorna o hash code do GUID.
            // Se dois objetos são iguais (Equals retorna true),
            // eles DEVEM ter o mesmo hash code.
            return this.guid.GetHashCode();
        }

        // Opcional: Sobrescrever o operador == para conveniência
        public static bool operator ==(Link link1, Link link2)
        {
            if (ReferenceEquals(link1, null))
            {
                return ReferenceEquals(link2, null);
            }
            return link1.Equals(link2);
        }

        public static bool operator !=(Link link1, Link link2)
        {
            return !(link1 == link2);
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

                    link._directionAuxiliary = (link.end.position - link.start.position).Normalized();
                }
            }

            processedOpLinks.Clear();

            foreach (Link link in links.Where(link => link.isAutoLink))
            {
                List<Link> adjacentLinks = links.Where(adjacentLink => (adjacentLink.start == link.start || adjacentLink.end == link.end) && adjacentLink != link && !adjacentLink.isInitialLink).ToList();

                Vector2D linkDirection = AcomodateAutoInitialLink(link.start, adjacentLinks, drawingDir);

                link._directionAuxiliary = linkDirection;
            }

            Link initialLink = links.Find(x => x.isInitialLink);
            List<Link> iAdjacentLinks = links.Where(adjacentLink => (adjacentLink.start == initialLink.start || adjacentLink.end == initialLink.end) && adjacentLink != initialLink).ToList();
            Vector2D initialLinkDirection = AcomodateAutoInitialLink(initialLink.start, iAdjacentLinks, drawingDir);
            initialLink._directionAuxiliary = initialLinkDirection;
            initialLink.radiusPercentage = drawingDir.TotalRadius();
        }
        
        public static Vector2D AcomodateAutoInitialLink(Node currentNode, List<Link> adjacentLinks, DrawingDir drawingDir)
        {
            List<Vector2D> adjacentDirections = new List<Vector2D>();

            Vector2D directionAdjacentAutoLinkStart = new Vector2D();
            Vector2D directionAdjacentAutoLinkEnd = new Vector2D();

            foreach (Link adjacentLink in adjacentLinks)
            {
                if (adjacentLink.isAutoLink)
                {
                    double radiusArcLink = drawingDir.AutoRadius();
                    double autoRatio = drawingDir.autoLinkDistanceRatio;
                    double nodeRadius = drawingDir.TotalRadius();
                    double distanceNodeAutoCenter = autoRatio * radiusArcLink + nodeRadius;
                    double angleAutoLinkStart = Vector2D.AngleBetween(nodeRadius, distanceNodeAutoCenter, radiusArcLink);

                    Vector2D positionNode = adjacentLink.start.position;
                    Vector2D directionAutoLinkCenter = adjacentLink._directionAuxiliary;
                    Vector2D positionAutoLinkCenter = positionNode + directionAutoLinkCenter * distanceNodeAutoCenter;

                    Vector2D positionTouchStart = positionAutoLinkCenter - directionAutoLinkCenter.Rotated(-angleAutoLinkStart) * radiusArcLink;
                    Vector2D positionTouchEnd = positionAutoLinkCenter - directionAutoLinkCenter.Rotated(angleAutoLinkStart) * radiusArcLink; ;

                    directionAdjacentAutoLinkStart = (positionTouchStart - positionNode).Normalized();
                    directionAdjacentAutoLinkEnd = (positionTouchStart - positionNode).Normalized();

                    adjacentDirections.Add(directionAdjacentAutoLinkStart);
                    adjacentDirections.Add(directionAdjacentAutoLinkEnd);
                }
                else if (adjacentLink.radiusPercentage == 0)
                {
                    Vector2D positionMiddle = adjacentLink.start.position.Middle(adjacentLink.end.position);
                    Vector2D directionTransition = (positionMiddle - currentNode.position).Normalized();
                    adjacentDirections.Add(directionTransition);
                }
                else
                {
                    Vector2D positionStartNode = adjacentLink.start.position;
                    Vector2D positionEndNode = adjacentLink.end.position;

                    Vector2D directionTransition = (positionEndNode - positionStartNode).Normalized();
                    Vector2D directionPerpendicular = directionTransition.Perpendicular();

                    Vector2D positionTransitionMiddle = positionStartNode.Middle(positionEndNode);

                    double distanceTransition = (positionEndNode - positionStartNode).Length();
                    double radiusPercentage = adjacentLink.radiusPercentage;

                    Vector2D positionArcSide = positionTransitionMiddle + directionPerpendicular * radiusPercentage * distanceTransition / 2;
                    Vector2D positionArcCenter = Vector2D.FindCenter(positionStartNode, positionArcSide, positionEndNode);

                    double radiusArcLink = (positionStartNode - positionArcCenter).Length();
                    int directionRotation = radiusPercentage < 0 ? 1 : -1;
                    double angleNodeOffset = Vector2D.AngleBetween(drawingDir.TotalRadius(), radiusArcLink, radiusArcLink);

                    Vector2D directionCenterToStartNode = (positionStartNode - positionArcCenter).Normalized();
                    Vector2D directionCenterToEndNode = (positionEndNode - positionArcCenter).Normalized();

                    Vector2D positionTouchStart = positionArcCenter + directionCenterToStartNode.Rotated(angleNodeOffset * directionRotation) * radiusArcLink;
                    Vector2D positionTouchEnd = positionArcCenter + directionCenterToEndNode.Rotated(angleNodeOffset * -directionRotation) * radiusArcLink;

                    Vector2D adjacentDirection = new Vector2D();

                    if (currentNode == adjacentLink.start)
                    {
                        adjacentDirection = (positionTouchStart - positionStartNode).Normalized();
                    }
                    else
                    {
                        adjacentDirection = (positionTouchEnd - positionEndNode).Normalized();
                    }

                    adjacentDirections.Add(adjacentDirection);
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

                if (currentVector == directionAdjacentAutoLinkEnd && nextVector == directionAdjacentAutoLinkStart)
                    continue;

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
                double distanceNodeAutoCenter = drawingDir.autoLinkDistanceRatio * drawingDir.AutoRadius() + drawingDir.TotalRadius();
                Vector2D positionAutoCenter = start.position + _directionAuxiliary * distanceNodeAutoCenter;

                double radius = drawingDir.AutoRadius() + 2 * drawingDir.textDistance;
                Vector2D topLeft = new Vector2D(positionAutoCenter.x - radius, positionAutoCenter.y + radius);
                Vector2D bottomRight = new Vector2D(positionAutoCenter.x + radius, positionAutoCenter.y - radius);

                return new Box(topLeft, bottomRight);
            }
            else if (isInitialLink)
            {
                Vector2D positionTransitionEnd = end.position + _directionAuxiliary * drawingDir.TotalRadius();
                Vector2D positionTransitionStart = positionTransitionEnd + _directionAuxiliary * radiusPercentage;

                Vector2D directionTransition = (positionTransitionEnd - positionTransitionStart).Normalized();
                Vector2D directionPerpendicular = directionTransition.Perpendicular();

                double arrowWidth = drawingDir.arrowWidth;

                Vector2D positionTopLeft = positionTransitionStart - directionPerpendicular * arrowWidth / 1.5;
                Vector2D positionBottomRight = positionTransitionEnd + directionPerpendicular * arrowWidth / 1.5;

                return Box.EncompassingBox(positionTopLeft, positionBottomRight);
            }
            else
            {
                if (radiusPercentage == 0)
                {

                }
                Vector2D perpendicular = (end.position - start.position).Perpendicular();
                Vector2D middle = start.position.Middle(end.position);

                double radiusTransition = (end.position - start.position).Length() / 2;
                Vector2D positionSide = middle + perpendicular * (radiusPercentage * radiusTransition + 40);

                return Box.EncompassingBox(start.position, positionSide, end.position);
            }
        }
        
        string GenerateArcTransitionSVG(SvgCanvas canvas, DrawingDir drawingDir)
        {
            string svgArc = "";
            Vector2D positionStartNode = canvas.ToSvgCoordinates(start.position);
            Vector2D positionEndNode = canvas.ToSvgCoordinates(end.position);

            Vector2D directionTransition = (positionEndNode - positionStartNode).Normalized();
            Vector2D directionPerpendicular = directionTransition.PerpendicularSVG();

            Vector2D positionTransitionMiddle = positionStartNode.Middle(positionEndNode);
            Vector2D position_directionAuxiliary = canvas.ToSvgCoordinates(_directionAuxiliary);

            double distanceTransition = (positionEndNode - positionStartNode).Length();

            Vector2D positionArcSide = positionTransitionMiddle + directionPerpendicular * radiusPercentage * distanceTransition / 2;
            Vector2D positionArcCenter = Vector2D.FindCenter(positionStartNode, positionArcSide, positionEndNode);

            double radiusArcLink = (positionStartNode - positionArcCenter).Length();

            Vector2D directionMiddleToCenter = (positionArcCenter - positionTransitionMiddle).Normalized();
            Vector2D directionMiddleToSide = (positionArcSide - positionTransitionMiddle).Normalized();

            int sweepFlag = radiusPercentage >= 0 ? 0 : 1;

            double angleNodeOffset = Vector2D.AngleBetween(drawingDir.nodeRadius, radiusArcLink, radiusArcLink);
            double angleArrowOffset = Vector2D.AngleBetween(drawingDir.arrowLength, radiusArcLink, radiusArcLink);

            int directionRotation = sweepFlag == 1 ? 1 : -1;

            Vector2D directionCenterToStartNode = (positionStartNode - positionArcCenter).Normalized();
            Vector2D directionCenterToEndNode = (positionEndNode - positionArcCenter).Normalized();

            Vector2D positionArcStart = positionArcCenter + directionCenterToStartNode.Rotated(angleNodeOffset * directionRotation) * radiusArcLink;
            Vector2D positionArcEnd = positionArcCenter + directionCenterToEndNode.Rotated((angleNodeOffset + angleArrowOffset) * -directionRotation) * radiusArcLink;

            Vector2D directionCenterArcStart = (positionArcStart - positionArcCenter).Normalized();
            Vector2D directionCenterArcEnd = (positionArcEnd - positionArcCenter).Normalized();
            double arcAngle = directionCenterArcStart.UnsignedRotationAngle(directionCenterArcEnd);


            int largeArcFlag = 1;

            if ((radiusPercentage >= 0 && arcAngle <= Math.PI) || (radiusPercentage <= 0 && arcAngle >= Math.PI))
                largeArcFlag = 1;
            else
                largeArcFlag = 0;


            Vector2D positionText = positionArcSide + directionPerpendicular * 20;

            Vector2D positionArrowTip = positionArcCenter + directionCenterToEndNode.Rotated(angleNodeOffset * -directionRotation) * radiusArcLink;

            Vector2D positionArrowBase = positionArcEnd;
            Vector2D directionArrow = (positionArrowTip - positionArrowBase).Normalized();
            Vector2D directionArrowPerpendicular = directionArrow.Perpendicular();

            Vector2D positionRightWing = positionArrowBase + directionArrowPerpendicular * drawingDir.arrowWidth / 2;
            Vector2D positionLeftWing = positionArrowBase - directionArrowPerpendicular * drawingDir.arrowWidth / 2;
            
            // Construct the arc's path attribute.
            string arcPath = $"M {positionArcStart.x} {positionArcStart.y} A {radiusArcLink} {radiusArcLink} 0 {largeArcFlag} {sweepFlag} {positionArcEnd.x} {positionArcEnd.y}";

            // Line attributes.
            string stroke = $"stroke=\"{drawingDir.strokeColor}\"";
            string strokeWidth = $"stroke-width=\"{drawingDir.linkStrokeWidth}\"";
            string fill = "fill=\"none\"";

            svgArc += $"<path d=\"{arcPath}\" {stroke} {strokeWidth} {fill} />{Environment.NewLine}";

            if (isControllable)
            {
                Vector2D positionTic = positionArcSide;
                Vector2D directionTicPerpendicular = directionPerpendicular;

                Vector2D positionTicStart = positionTic - directionTicPerpendicular * drawingDir.ticLength / 2;
                Vector2D positionTicEnd = positionTic + directionTicPerpendicular * drawingDir.ticLength / 2;
                svgArc += $"<line x1=\"{positionTicStart.x}\" y1=\"{positionTicStart.y}\" x2=\"{positionTicEnd.x}\" y2=\"{positionTicEnd.y}\" stroke=\"{drawingDir.strokeColor}\" stroke-width=\"2\" />{Environment.NewLine}";
            }

            svgArc += GenerateArrowheadPolygonSVG(positionArrowTip, positionArrowBase, drawingDir);
            svgArc += GenerateSvgTextElement(positionText, directionPerpendicular, canvas, name, drawingDir);
            
            
            return svgArc;
        }

        string GenerateArcTransitionLatex(DrawingDir drawingDir)
        {
            string latexArc = "";
            
            Vector2D transitionDirection = (end.position - start.position).Normalized();
            Vector2D perpendicularDirection = transitionDirection.Perpendicular();
            Vector2D middlePoint = start.position.Middle(end.position);

            double distanceTransition = (end.position - start.position).Length();

            Vector2D positionSide = middlePoint + perpendicularDirection * radiusPercentage * distanceTransition / 2;
            
            Vector2D center = Vector2D.FindCenter(start.position, end.position, positionSide);

            double arcRadius = (positionSide - center).Length();

            // Find the arrow's tip and base points by rotating the vector from the arc's center 
            // by the angles formed by the node radius and the arrow length, respectively.
            // Note: Since the rotation is counterclockwise, we must use negative angles.
            double nodeRadiusAngle = Vector2D.AngleBetween(drawingDir.nodeRadius, arcRadius, arcRadius);
            double arrowBaseAngle = Vector2D.AngleBetween(drawingDir.arrowLength, arcRadius, arcRadius);

            int factorAngleCorretion = radiusPercentage > 0 ? 1 : -1;

            Vector2D arrowTip = center + (end.position - center).Rotated(-nodeRadiusAngle * factorAngleCorretion);
            Vector2D arrowBase = center + (end.position - center).Rotated(-(nodeRadiusAngle + arrowBaseAngle) * factorAngleCorretion);

            Vector2D tempDirection = (start.position - center);
            Vector2D arcOrigin = center + (start.position - center).Rotated(nodeRadiusAngle * factorAngleCorretion);
            Vector2D arcDestination = arrowBase;
            
            Vector2D textPosition = positionSide + perpendicularDirection * drawingDir.textDistance;

            Vector2D directionMiddleToCenter = (center - middlePoint).Normalized();
            Vector2D directionMiddleToSide = (positionSide - middlePoint).Normalized();
            
            // Between two points exist four possible arcs in SVG.
            // largeArcFlag determinnes if the arc will be the smallest or biggest between the points.
            // sweepFlag determines if the arc will have a clockwise or counter-clockwise rotation.
            int largeArcFlag = directionMiddleToCenter == directionMiddleToSide ? 1 : 0;
            int sweepFlag = radiusPercentage >= 0 ? 0 : 1;

            Vector2D directionCenterToStart = (arcOrigin - center).Normalized();
            Vector2D directionCenterToEnd = (arcDestination - center).Normalized();

            double angleAutoLinkStart = (arcOrigin - center).UnsignedAngleDegrees();
            double angleAutoLinkEnd = (arcDestination - center).UnsignedAngleDegrees();

            double angleSmaller = Vector2D.SmallestAngleBetweenVectorsDegrees(directionCenterToStart, directionCenterToEnd);
            double angleBigger = 360 - angleSmaller;

            double angleToInclude = largeArcFlag == 1 ? angleBigger : angleSmaller;
            int includeOperation = sweepFlag == 1 ? -1 : 1;

            angleAutoLinkEnd = angleAutoLinkStart + includeOperation * angleToInclude;
            
            latexArc += @"\draw [black] (" + arcOrigin.x / 10 + "," + arcOrigin.y / 10 + ") arc (" + angleAutoLinkStart + ":" + angleAutoLinkEnd + ":" + arcRadius / 10 + ");" + Environment.NewLine;

            if (isControllable)
            {
                Vector2D positionTic = positionSide;
                Vector2D directionTicPerpendicular = perpendicularDirection;

                Vector2D positionTicStart = positionTic - directionTicPerpendicular * drawingDir.ticLength / 2;
                Vector2D positionTicEnd = positionTic + directionTicPerpendicular * drawingDir.ticLength / 2;

                latexArc += @"\draw [black] (" + positionTicStart.x / 10 + "," + positionTicStart.y / 10 + ") -- (" + positionTicEnd.x / 10 + "," + positionTicEnd.y / 10 + ");" + Environment.NewLine; 
            }

            latexArc += GenerateArrowheadPolygonLatex(arrowTip, arrowBase, drawingDir);
            latexArc += GenerateLatexTextElement(textPosition, perpendicularDirection, name, drawingDir);
            
            
            return latexArc;
        }
        
        string GenerateStraightTransitionSVG(SvgCanvas canvas, DrawingDir drawingDir)
        {
            Vector2D lineOrigin = new Vector2D();
            Vector2D lineDestination = new Vector2D();

            Vector2D arrowTip = new Vector2D();
            Vector2D arrowBase = new Vector2D();

            Vector2D textPosition = new Vector2D();

            Vector2D directionPerpendicular = new Vector2D();

            if (isInitialLink)
            {
                Vector2D positionNode = canvas.ToSvgCoordinates(start.position);

                Vector2D directionTransition = -_directionAuxiliary.ToSvgDirection();
                directionPerpendicular = directionTransition.PerpendicularSVG();

                arrowTip = positionNode - directionTransition * drawingDir.TotalRadius();
                arrowBase = positionNode - directionTransition * (drawingDir.TotalRadius() + drawingDir.arrowLength);

                lineOrigin = arrowBase - directionTransition * radiusPercentage;
                lineDestination = arrowBase;
            }
            else
            {
                Vector2D positionStartNode = canvas.ToSvgCoordinates(start.position);
                Vector2D positionEndNode = canvas.ToSvgCoordinates(end.position);

                Vector2D directionTransition = (positionEndNode - positionStartNode).Normalized();
                directionPerpendicular = directionTransition.PerpendicularSVG();

                Vector2D positionMiddle = positionStartNode.Middle(positionEndNode);

                arrowTip = positionEndNode - directionTransition * drawingDir.nodeRadius;
                arrowBase = positionEndNode - directionTransition * (drawingDir.nodeRadius + drawingDir.arrowLength);

                lineOrigin = positionStartNode + directionTransition * drawingDir.nodeRadius;
                lineDestination = arrowBase;

                textPosition = positionMiddle + directionPerpendicular * drawingDir.textDistance;
            }
            
            
            // Convert the points to the SVG canva's coordinate system.
            /* arrowTip = canvas.ToSvgCoordinates(arrowTip);
            arrowBase = canvas.ToSvgCoordinates(arrowBase);
            lineOrigin = canvas.ToSvgCoordinates(lineOrigin);
            lineDestination = canvas.ToSvgCoordinates(lineDestination); */
            
            string svgLine = $"<line x1=\"{lineOrigin.x}\" y1=\"{lineOrigin.y}\" x2=\"{lineDestination.x}\" y2=\"{lineDestination.y}\" stroke=\"{drawingDir.strokeColor}\" stroke-width=\"{drawingDir.linkStrokeWidth}\" />{Environment.NewLine}";

            if (isControllable)
            {
                Vector2D positionTic = lineOrigin.Middle(arrowTip);
                Vector2D directionTicPerpendicular = directionPerpendicular;

                Vector2D positionTicStart = positionTic - directionTicPerpendicular * drawingDir.ticLength / 2;
                Vector2D positionTicEnd = positionTic + directionTicPerpendicular * drawingDir.ticLength / 2;
                svgLine += $"<line x1=\"{positionTicStart.x}\" y1=\"{positionTicStart.y}\" x2=\"{positionTicEnd.x}\" y2=\"{positionTicEnd.y}\" stroke=\"{drawingDir.strokeColor}\" stroke-width=\"2\" />{Environment.NewLine}";
            }
            
            svgLine += GenerateArrowheadPolygonSVG(arrowTip, arrowBase, drawingDir);
            if (isInitialLink) return svgLine;
            
            svgLine += GenerateSvgTextElement(textPosition, directionPerpendicular, canvas, name, drawingDir);

            return svgLine;
        }

        string GenerateStraightTransitionLatex(DrawingDir drawingDir)
        {
            Vector2D transitionDirection = (end.position - start.position).Normalized();
            if (isInitialLink) transitionDirection = -_directionAuxiliary;
            
            Vector2D perpendicularDirection = transitionDirection.Perpendicular();
            
            Vector2D arrowTip = end.position - transitionDirection * drawingDir.TotalRadius();
            Vector2D arrowBase = end.position - transitionDirection * (drawingDir.TotalRadius() + drawingDir.arrowLength);

            Vector2D lineOrigin = start.position + transitionDirection * drawingDir.TotalRadius();
            if (isInitialLink) lineOrigin = arrowBase - transitionDirection * radiusPercentage;
            
            Vector2D lineDestination = arrowBase;

            Vector2D positionMiddle = lineOrigin.Middle(arrowTip);

            Vector2D textPosition = positionMiddle + perpendicularDirection * drawingDir.textDistance;

            string latexLineElement = @"\draw [black] (" + lineOrigin.x / 10 + "," + lineOrigin.y / 10 + ") -- (" + lineDestination.x / 10 + "," + lineDestination.y / 10 + ");" + Environment.NewLine; 

            if (isControllable)
            {
                Vector2D positionTic = lineOrigin.Middle(arrowTip);
                Vector2D directionTicPerpendicular = perpendicularDirection;

                Vector2D positionTicStart = positionTic - directionTicPerpendicular * drawingDir.ticLength / 2;
                Vector2D positionTicEnd = positionTic + directionTicPerpendicular * drawingDir.ticLength / 2;
                latexLineElement += @"\draw [black] (" + positionTicStart.x / 10 + "," + positionTicStart.y / 10 + ") -- (" + positionTicEnd.x / 10 + "," + positionTicEnd.y / 10 + ");" + Environment.NewLine; 
            }

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

            if (isControllable)
            {
                Vector2D positionTic = positionAutoLinkCenter + directionAutoLinkCenter * autoRadius;
                Vector2D directionTicPerpendicular = directionAutoLinkCenter;

                Vector2D positionTicStart = positionTic - directionTicPerpendicular * drawingDir.ticLength / 2;
                Vector2D positionTicEnd = positionTic + directionTicPerpendicular * drawingDir.ticLength / 2;
                autoLinkSVG += $"<line x1=\"{positionTicStart.x}\" y1=\"{positionTicStart.y}\" x2=\"{positionTicEnd.x}\" y2=\"{positionTicEnd.y}\" stroke=\"{drawingDir.strokeColor}\" stroke-width=\"2\" />{Environment.NewLine}";
            }
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
            
            Vector2D textPosition = positionAutoLinkCenter + transitionDirection * (drawingDir.AutoRadius() + 20);

            /* double angleAutoLinkStart = (originPoint - positionAutoLinkCenter).UnsignedAngle();
            double angleAutoLinkEnd = (endPoint - positionAutoLinkCenter).UnsignedAngle(); */

            int largeArcFlag = 1;
            int sweepFlag = 0;

            Vector2D directionCenterToStart = (originPoint - positionAutoLinkCenter).Normalized();
            Vector2D directionCenterToEnd = (endPoint - positionAutoLinkCenter).Normalized();

            double angleAutoLinkStart = (originPoint - positionAutoLinkCenter).UnsignedAngleDegrees();
            double angleAutoLinkEnd = (endPoint - positionAutoLinkCenter).UnsignedAngleDegrees();

            double angleSmaller = Vector2D.SmallestAngleBetweenVectorsDegrees(directionCenterToStart, directionCenterToEnd);
            double angleBigger = 360 - angleSmaller;

            double angleToInclude = largeArcFlag == 1 ? angleBigger : angleSmaller;
            int includeOperation = sweepFlag == 1 ? -1 : 1;

            angleAutoLinkEnd = angleAutoLinkStart + includeOperation * angleToInclude;
            
            autoLinkSVG += @"\draw [black] (" + originPoint.x / 10 + "," + originPoint.y / 10 + ") arc (" + angleAutoLinkStart + ":" + angleAutoLinkEnd + ":" + autoRadius / 10 + ");" + Environment.NewLine;
            if (isControllable)
            {
                Vector2D positionTic = positionAutoLinkCenter + transitionDirection * autoRadius;
                Vector2D directionTicPerpendicular = transitionDirection;

                Vector2D positionTicStart = positionTic - directionTicPerpendicular * drawingDir.ticLength / 2;
                Vector2D positionTicEnd = positionTic + directionTicPerpendicular * drawingDir.ticLength / 2;
                autoLinkSVG += @"\draw [black] (" + positionTicStart.x / 10 + "," + positionTicStart.y / 10 + ") -- (" + positionTicEnd.x / 10 + "," + positionTicEnd.y / 10 + ");" + Environment.NewLine; 
            }
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
