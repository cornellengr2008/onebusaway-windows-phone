﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;

namespace OneBusAway.WP7.ViewModel
{
    public class BusDirectionVM : AViewModel
    {
        private Object routeDirectionsLock;
        public ObservableCollection<RouteStops> RouteDirections { get; private set; }

        private int pendingRouteDirectionsCount;
        private List<RouteStops> pendingRouteDirections;

        #region Constructors

        public BusDirectionVM()
            : base()
        {
            Initialize();
        }

        public BusDirectionVM(IBusServiceModel busServiceModel)
            : base(busServiceModel)
        {
            Initialize();
        }

        private void Initialize()
        {
            routeDirectionsLock = new Object();
            pendingRouteDirections = new List<RouteStops>();
            pendingRouteDirectionsCount = 0;
            RouteDirections = new ObservableCollection<RouteStops>();
        }

        #endregion

        public void LoadRouteDirections(List<Route> routes)
        {
            lock (routeDirectionsLock)
            {
                RouteDirections.Clear();
                pendingRouteDirections.Clear();
            }

            pendingRouteDirectionsCount += routes.Count;
            pendingOperations += routes.Count;
            foreach(Route route in routes)
            {
                busServiceModel.StopsForRoute(route);
            }
        }

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                lock (routeDirectionsLock)
                {
                    e.routeStops.ForEach(routeStop => pendingRouteDirections.Add(routeStop));

                    // Subtract 1 because we haven't decremented the count yet
                    if (pendingRouteDirectionsCount - 1 == 0)
                    {
                        if (LocationKnown == true)
                        {
                            pendingRouteDirections.Sort(new RouteStopsDistanceComparer(CurrentLocation));
                        }

                        pendingRouteDirections.ForEach(route => RouteDirections.Add(route));
                    }
                }
            }

            pendingRouteDirectionsCount--;
            pendingOperations--;
        }

        public override void RegisterEventHandlers()
        {
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);

            pendingOperations = 0;
        }
    }
}
