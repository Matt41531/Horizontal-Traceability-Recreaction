import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.*;

public class Formatter {

    private ArrayList<String> callgraph;

    public Formatter(ArrayList<String> callgraph){
    
        this.callgraph = callgraph;
    
    }
    
    public ArrayList<String> ProcessCallGraph(){
    
        ArrayList<String> processedString = new ArrayList<String>();
        
        for(int i = 0; i < callgraph.size(); i++){
        
            String temp = callgraph.get(i);
            String firstString = "";
            String secondString = "";
            String thirdString = "";
            
            boolean initFlag = false;
            
            String[] firstArray;
            String[] secondArray;
            
            int begin = 0;
            int mid = 0;
            int end = temp.length();
            
            
            mid = temp.indexOf("||");
            
            firstString = temp.substring(begin, mid);
            thirdString = temp.substring(mid + 2, end) ;
            
            firstArray = firstString.split("\\.");
            secondArray = thirdString.split("\\.");
            
            //first part
            firstString = firstArray[firstArray.length - 2];
            
            //second part
            secondString = firstArray[firstArray.length - 1];
            
            //removing parentesis
            if(secondString.contains("(")){
                
                int counter = secondString.indexOf("(");
                secondString = secondString.substring(0, counter);
            
            }
            
            //preparing for output
            if(firstString.equals(secondString)){
            
                processedString.add(firstString +"\': "+thirdString);
            
            }else{
            
                processedString.add(firstString + ":" + secondString +"\': "+thirdString);
            }
            
         
        }
        
        
        return processedString;
    
    }
    
}

