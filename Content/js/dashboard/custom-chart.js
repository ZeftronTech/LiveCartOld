// line chart
$(function() {

		var data = [{
			label: "United States",
			data: [[1990, 18.9], [1991, 18.7], [1992, 18.4], [1993, 19.3], [1994, 19.5], [1995, 19.3], [1996, 19.4], [1997, 20.2], [1998, 19.8], [1999, 19.9], [2000, 20.4], [2001, 20.1], [2002, 20.0], [2003, 19.8], [2004, 20.4]]
		},  {
			label: "Sweden",
			data: [[1990, 5.8], [1991, 6.0], [1992, 5.9], [1993, 5.5], [1994, 5.7], [1995, 5.3], [1996, 6.1], [1997, 5.4], [1998, 5.4], [1999, 5.1], [2000, 5.2], [2001, 5.4], [2002, 6.2], [2003, 5.9], [2004, 5.89]]
		}, {
			label: "Norway",
			data: [[1990, 8.3], [1991, 8.3], [1992, 7.8], [1993, 8.3], [1994, 8.4], [1995, 5.9], [1996, 6.4], [1997, 6.7], [1998, 6.9], [1999, 7.6], [2000, 7.4], [2001, 8.1], [2002, 12.5], [2003, 9.9], [2004, 19.0]]
		}];

		var options = {
			series: {
				lines: {
					show: true
				},
				points: {
					show: false
				}
			},
			legend: {
				noColumns: 4
			},
			xaxis: {
				tickDecimals: 0
			},
			yaxis: {
				min: 0
			},
			selection: {
				mode: "x"
			}
		};

		var placeholder = $("#placeholder");



		var plot = $.plot(placeholder, data, options);
		// Add the Flot version string to the footer
		
	});
	
	//chart1
	$(function() {

		var d1 = [];
		for (var i = 0; i <= 10; i += 1) {
			d1.push([i, parseInt(Math.random() * 30)]);
		}

		var d2 = [];
		for (var i = 0; i <= 10; i += 1) {
			d2.push([i, parseInt(Math.random() * 30)]);
		}

		var d3 = [];
		for (var i = 0; i <= 10; i += 1) {
			d3.push([i, parseInt(Math.random() * 30)]);
		}

		var stack = 0,
			bars = true,
			lines = false,
			steps = false;

		function plotWithOptions() {
			$.plot("#sensing", [ d1, d2, d3 ], {
				series: {
					stack: stack,
					lines: {
						show: lines,
						fill: true,
						steps: steps
					},
					bars: {
						show: bars,
						barWidth: 0.6
					}
				}
			});
		}

		plotWithOptions();

		// Add the Flot version string to the footer

		
	});
	
	//chart 2
	
		$(function() {		
	var my_data = [[0, 3], [1, 6], [2, 5], [3, 2], [4, 8]];  
  
$.plot($("#offers"), [  
{  
    data: my_data,  
    bars: {  
        show: true,  
        horizontal: true  
    }  
}  
]); 	
		
	});
	
		//chart 3
	
		$(function() {		
	var my_data = [[3, 2], [2, 4]];  
  
$.plot($("#consumer"), [  
{  
    data: my_data,  
    bars: {  
        show: true,  
        horizontal: true  
    }  
}  
]); 	
		
	});
	
		