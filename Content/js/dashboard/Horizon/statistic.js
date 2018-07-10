//generate random number for charts
//randNum = function(){
//	return Math.floor(Math.random()*101);
//	//return (Math.floor( Math.random()* (1+40-20) ) ) + 20;
//}

//var chartColours = ["#58ACFA", "#FF8000", "#A4A4A4", "#F5DA81", "#06b84d", '#5a8022', '#2c7282'];


//// document ready function
//$(document).ready(function() {

//	var divElement = $('div'); //log all div elements

//	

//	

//    

//	

//	//------------- Graphs for chart.html page -------------//

//	

//	


//	

//	



//	

//	//Horizontal bars chart
//    if (divElement.hasClass('horizontal-bars-chart')) {
//	$(function () {
//		//some data
//		//Display horizontal graph
//    var d1_h = [];
//    for (var i = 0; i <= 5; i += 1)
//        d1_h.push([4,i ]);

//    var d2_h = [];
//    for (var i = 0; i <= 5; i += 1)
//        d2_h.push([4,i ]);

//    var d3_h = [];
//    for (var i = 0; i <= 5; i += 1)
//        d3_h.push([ 6,i]);
//                
//    var ds_h = new Array();
//    ds_h.push({
//        data: lineOfferW3.data,
//        label:  "1st Week",
//		bars: {
//            horizontal:true, 
//            show: true, 
//            barWidth: 0.2, 
//            order: 1,
//        }
//    });
//	ds_h.push({
//	    data:lineOfferW4.data,
//        label:  "2nd Week",
//	    bars: {
//	        horizontal:true, 
//	        show: true, 
//	        barWidth: 0.2, 
//	        order: 2
//	    }
//	});
//	ds_h.push({
//	    data:lineOfferW3.data,
//        label:  lineOfferW3.label,
//	    bars: {
//	        horizontal:true, 
//	        show: true, 
//	        barWidth: 0.2, 
//	        order: 3
//	    }
//	});
//    ds_h.push({
//	    data: lineOfferW4.data,
//        label:  lineOfferW4.label,
//	    bars: {
//	        horizontal:true, 
//	        show: true, 
//	        barWidth: 0.2, 
//	        order: 4
//	    }
//	});


//		var options = {
//				grid: {
//					show: true,
//				    aboveData: false,
//				    color: "#3f3f3f" ,
//				    labelMargin: 5,
//				    axisMargin: 0, 
//				    borderWidth: 0,
//				    borderColor:null,
//				    minBorderMargin: 5 ,
//				    clickable: true, 
//				    hoverable: true,
//				    autoHighlight: false,
//				    mouseActiveRadius: 20
//				},
//		        series: {
//		        	grow: {active:false},
//			        bars: {
//			        	show:true,
//						horizontal: true,
//						barWidth:0.2,
//						fill:1
//					}
//		        },
//		        legend: { position: "ne" },
//		        colors: chartColours,
//		        tooltip: true, //activate tooltip
//				tooltipOpts: {
//					content: "%y.0 : %x.0",
//					shifts: {
//						x: -30,
//						y: -50
//					}
//				}
//		};

//		$.plot($(".horizontal-bars-chart"), ds_h, options);
//	});
//	}//end if

//	

//});

